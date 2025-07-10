using Pastel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace GCodeParser
{
  public partial class frmMain : Form
  {
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AllocConsole();


    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    public frmMain()
    {
      InitializeComponent();
    }

    private void frmMain_Load(object sender, EventArgs e)
    {
      AllocConsole();

      int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
      int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;

      // Move console to upper-left
      IntPtr consoleHandle = GetConsoleWindow();
      MoveWindow(consoleHandle, 0, 0, screenWidth / 2, screenHeight, true);

      // Move form to upper-right
      this.StartPosition = FormStartPosition.Manual;
      this.Location = new System.Drawing.Point(screenWidth / 2, 0);
      this.Size = new System.Drawing.Size(screenWidth / 2, screenHeight / 2);



      //Quick test:
      //cPose testPose = new cPose(0, 0, 0, -180.0, 90.2, -90.0);
      ////cPose testPose = new cPose(0, 0, 0, -90.0, 10, -180.0);
      //Console.WriteLine($"Test Pose ZYX: X={testPose.X:0.000} Y={testPose.Y:0.000} Z={testPose.Z:0.000} RZ={testPose.rZ:0.000} RY={testPose.rY:0.000} RX={testPose.rX:0.000}".Pastel(Color.Green));
      //cLHT test = new cLHT();
      //test.setTransformFromEulerZYX(testPose);
      //cPose testPoseEulerXYZ = test.getPoseEulerXYZ();
      //Console.WriteLine($"Test Pose XYZ: X={testPoseEulerXYZ.X:0.000} Y={testPoseEulerXYZ.Y:0.000} Z={testPoseEulerXYZ.Z:0.000} RX={testPoseEulerXYZ.rX:0.000} RY={testPoseEulerXYZ.rY:0.000} RZ={testPoseEulerXYZ.rZ:0.000}".Pastel(Color.Green));
      //test.setTransformFromEulerXYZ(testPoseEulerXYZ);
      //cPose testBackTOZYX = test.getPoseEulerZYX();
      //Console.WriteLine($"Test Back to ZYX: X={testBackTOZYX.X:0.000} Y={testBackTOZYX.Y:0.000} Z={testBackTOZYX.Z:0.000} RZ={testBackTOZYX.rZ:0.000} RY={testBackTOZYX.rY:0.000} RX={testBackTOZYX.rX:0.000}".Pastel(Color.Green));
      //Console.WriteLine();
    }
    private void button3_Click(object sender, EventArgs e)
    {
      OpenFileDialog ofd = new OpenFileDialog();
      ofd.Filter = "GCode files (*.mpf)|*.mpf|All files (*.*)|*.*";
      ofd.Title = "Select GCode File";

      if (ofd.ShowDialog() != DialogResult.OK)
      {
        MessageBox.Show("No file selected.");
        return;
      }

      string[] lines = File.ReadAllLines(ofd.FileName);

      var g1Regex = new Regex(@"G1\s+.*", RegexOptions.Compiled);
      var g9Regex = new Regex(@"G9\s+.*", RegexOptions.Compiled);

      var fieldRegex = new Regex(@"X=([-.\d]+)|Y=([-.\d]+)|Z=([-.\d]+)|RZ=([-.\d]+)|RY=([-.\d]+)|RX=([-.\d]+)|ROTX=DC\(([-.\d]+)\)");

      List<string> outputLines = new List<string>();
      foreach (var line in lines)
      {
        bool isG1 = g1Regex.IsMatch(line);
        bool isG9 = g9Regex.IsMatch(line);
        if (isG9)
        {
          Console.WriteLine("got g9");
        }
        if (!(isG1 || isG9))
        {
          // If the line does not match G1 or G9, we just add it to output
          outputLines.Add(line);
          continue;
        }

        // Extract N number from start of line
        int nValue = 0;
        var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && parts[0].StartsWith("N") && int.TryParse(parts[0].Substring(1), out int parsedN))
        {
          nValue = parsedN;
        }

        // Extract field values
        var matches = fieldRegex.Matches(line);
        double[] values = new double[7]; // X, Y, Z, RZ, RY, RX, ROTX

        foreach (Match m in matches)
        {
          for (int i = 1; i <= 7; i++)
          {
            if (m.Groups[i].Success)
            {
              values[i - 1] = double.Parse(m.Groups[i].Value);
              break;
            }
          }
        }

        // ROTX_TRAFO(0)
        // TRAORI

        // Now create the cPose from parsed values
        cPose pose = new cPose
        {
          x = values[0],
          y = values[1],
          z = values[2],
          rz = values[3],
          ry = values[4],
          rx = values[5]
        };
        double W = values[6];
        cLHT lhtPose = new cLHT();
        lhtPose.setTransformFromEulerZYX(pose);
        cLHT Wcmd = new cLHT(0, 0, 0, -W.D2R(), 0, 0);
        cPose cartesianPose = (Wcmd * lhtPose).getPoseEulerXYZ();
        Console.WriteLine($"X: {cartesianPose.X:0.000} Y: {cartesianPose.Y:0.000} Z: {cartesianPose.Z:0.000} RZ: {cartesianPose.rZ:0.000} RY: {cartesianPose.rY:0.000} RX: {cartesianPose.rX:0.000} ROTX: {values[6]:0.000}".Pastel(Color.Green));

        cPose cartesianPoseEulerZYX = cartesianPose.getLHT().getPoseEulerZYX();
        // Create the output line        
        string outputLineType = isG1 ? "G1" : "G9";
        string outputLine = $"N{nValue} {outputLineType} X={cartesianPose.X:0.000} Y={cartesianPose.Y:0.000} Z={cartesianPose.Z:0.000} RZ={cartesianPose.rZ:0.000} RY={cartesianPose.rY:0.000} RX={cartesianPose.rX:0.000} ROTX={values[6]:0.000}";
        outputLines.Add(outputLine);
      }
      // Write the output lines to a new file
      string directory = Path.GetDirectoryName(ofd.FileName);
      string filenameWithoutExt = Path.GetFileNameWithoutExtension(ofd.FileName);
      string outputFileName = Path.Combine(directory, filenameWithoutExt + "_transformed.mpf");
      if (outputLines.Count > 0)
      {
        File.WriteAllLines(outputFileName, outputLines);
        Console.WriteLine($"Parsed G1 values written to {outputFileName}".Pastel(Color.Green));
      }
      else
      {
        Console.WriteLine("No G1 lines found.".Pastel(Color.OrangeRed));
      }
    }
    private void button1_Click(object sender, EventArgs e)
    {
      OpenFileDialog ofd = new OpenFileDialog();
      ofd.Filter = "GCode files (*.mpf)|*.mpf|All files (*.*)|*.*";
      ofd.Title = "Select GCode File";

      if (ofd.ShowDialog() != DialogResult.OK)
      {
        MessageBox.Show("No file selected.");
        return;
      }

      string[] lines = File.ReadAllLines(ofd.FileName);

      var g1Regex = new Regex(@"G1\s+.*", RegexOptions.Compiled);
      var fieldRegex = new Regex(@"X=([-.\d]+)|Y=([-.\d]+)|Z=([-.\d]+)|RZ=([-.\d]+)|RY=([-.\d]+)|RX=([-.\d]+)|ROTX=DC\(([-.\d]+)\)");

      var sb = new StringBuilder();
      sb.AppendLine("Transit\tN\tG1\tX\tY\tZ\tRZ\tRY\tRX\tROTX\teRX\teRY\teRZ");


      bool inTransit = false;
      bool afterSkipExit = false;
      string beginTransit = "; Begin transit";
      string endTransit = "; End transit";
      string skipExit = "SKIP_EXIT";
      int nTransit = 1;
      bool capturedStartPosition = false;
      cPose initialPose = new cPose();
      double initialU = 0;
      cPose lastPose = new cPose();
      double lastU = 0;

      foreach (var line in lines)
      {
        if (line.Contains(beginTransit))
        {
          inTransit = true;
        }
        if (inTransit && line.Contains(skipExit))
        {
          afterSkipExit = true;
        }

        if (line.Contains(endTransit))
        {
          inTransit = false;
          afterSkipExit = false;
          capturedStartPosition = false;
          nTransit++;
        }

        if (!(inTransit && afterSkipExit))
          continue;

        if (!g1Regex.IsMatch(line))
          continue;

        // Extract N number from start of line
        int nValue = 0;
        var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && parts[0].StartsWith("N") && int.TryParse(parts[0].Substring(1), out int parsedN))
        {
          nValue = parsedN;
        }

        // Extract field values
        var matches = fieldRegex.Matches(line);
        double[] values = new double[7]; // X, Y, Z, RZ, RY, RX, ROTX

        foreach (Match m in matches)
        {
          for (int i = 1; i <= 7; i++)
          {
            if (m.Groups[i].Success)
            {
              values[i - 1] = double.Parse(m.Groups[i].Value);
              break;
            }
          }
        }

        // Now create the cPose from parsed values
        cPose pose = new cPose
        {
          x = values[0],
          y = values[1],
          z = values[2],
          rz = values[3],
          ry = values[4],
          rx = values[5]
        };

        double Radius = Math.Sqrt(pose.y * pose.y + pose.z * pose.z);
        if (Radius > 195 && !capturedStartPosition) //we're at the safe radius, mark for replacement. 
        {
          initialPose = pose;
          initialU = values[6];
          capturedStartPosition = true;
        }
        if (capturedStartPosition && Radius < 195)
        {
          cTransform rU = new cTransform(0, 0, 0, -initialU, 0, 0);
          cPose xInitialPose = (rU * initialPose.getTransform()).getPose();

          cTransform rUend = new cTransform(0, 0, 0, -lastU, 0, 0);
          cPose xlastPose = (rUend * lastPose.getTransform()).getPose();

          Console.WriteLine("========================================".Pastel(Color.Yellow));
          Console.WriteLine($"Transit: {nTransit} N: {nValue}".Pastel(Color.Yellow));
          Console.WriteLine($"Initial Pose: {initialU:F3} ".Pastel(Color.Yellow));
          Console.WriteLine($"x: {initialPose.x:F3} y: {initialPose.y:F3} z: {initialPose.z:F3} rx: {initialPose.rx:F3} ry: {initialPose.ry:F3} rz: {initialPose.rz:F3}".Pastel(Color.Yellow));
          Console.WriteLine($"x: {xInitialPose.x:F3} y: {xInitialPose.y:F3} z: {xInitialPose.z:F3} rx: {xInitialPose.rx:F3} ry: {xInitialPose.ry:F3} rz: {xInitialPose.rz:F3}".Pastel(Color.Yellow));

          Console.WriteLine();
          Console.WriteLine($"Last Pose: {lastU:F3} ".Pastel(Color.Yellow));
          Console.WriteLine($"x: {lastPose.x:F3} y: {lastPose.y:F3} z: {lastPose.z:F3} rx: {lastPose.rx:F3} ry: {lastPose.ry:F3} rz: {lastPose.rz:F3}".Pastel(Color.Yellow));
          Console.WriteLine($"x: {xlastPose.x:F3} y: {xlastPose.y:F3} z: {xlastPose.z:F3} rx: {xlastPose.rx:F3} ry: {xlastPose.ry:F3} rz: {xlastPose.rz:F3}".Pastel(Color.Yellow));
          Console.WriteLine("========================================".Pastel(Color.Yellow));
          Console.WriteLine();

          // Output Dummy Code:
          Console.WriteLine("ROTX_TRAFO(0)".Pastel(Color.Green));
          Console.WriteLine($"G1 ROTX=DC({initialU:F3}) FA[ROTX]=4320.0 \\ \nX={xInitialPose.x:F3} Y={xInitialPose.y:F3} Z={xInitialPose.z:F3} RZ={xInitialPose.rz:F3} RY={xInitialPose.ry:F3} RX={xInitialPose.rx:F3} F=6000.0".Pastel(Color.Green));
          Console.WriteLine($"G1 ROTX=DC({lastU:F3}) FA[ROTX]=4320.0 \\ \nX={xlastPose.x:F3} Y={xlastPose.y:F3} Z={xlastPose.z:F3} RZ={xlastPose.rz:F3} RY={xlastPose.ry:F3} RX={xlastPose.rx:F3} F=6000.0".Pastel(Color.Green));
          Console.WriteLine("ROTX_TRAFO(1)".Pastel(Color.Green));
          //calculate move and go!
        }
        lastPose = pose;
        lastU = values[6];

        cLHT lht = new cLHT();
        lht.setTransformFromEulerZYX(pose);
        cPose poseEulerXYZ = lht.getPoseEulerXYZ();

        sb.AppendLine($"{nTransit}\t{nValue}\tG1\t{values[0]:0.000}\t{values[1]:0.000}\t{values[2]:0.000}\t{values[3]:0.000}\t{values[4]:0.000}\t{values[5]:0.000}\t{values[6]:0.000}\t{poseEulerXYZ.rx:0.000}\t{poseEulerXYZ.ry:0.000}\t{poseEulerXYZ.rz:0.000}");
      }

      if (sb.Length > 0)
      {
        Clipboard.SetText(sb.ToString());
        Console.WriteLine("Parsed G1 values copied to clipboard.".Pastel(Color.Green));
        Console.WriteLine($"There were {nTransit - 1} detected.".Pastel(Color.Green));
      }
      else
      {
        Console.WriteLine("No G1 lines found.".Pastel(Color.OrangeRed));
      }
    }


    private void btnCreateTransits(object sender, EventArgs e)
    {
      OpenFileDialog ofd = new OpenFileDialog();
      ofd.Filter = "GCode files (*.mpf)|*.mpf|All files (*.*)|*.*";
      ofd.Title = "Select GCode File";

      if (ofd.ShowDialog() != DialogResult.OK)
      {
        MessageBox.Show("No file selected.");
        return;
      }

      string[] lines = File.ReadAllLines(ofd.FileName);
      // create an output filename based on the input file tag with "_twr" suffix
      string directory = Path.GetDirectoryName(ofd.FileName);
      string filenameWithoutExt = Path.GetFileNameWithoutExtension(ofd.FileName);
      string outputFileName = Path.Combine(directory, filenameWithoutExt + "_twr.mpf");

      List<string> outputLines = new List<string>();

      var g1Regex = new Regex(@"G1\s+.*", RegexOptions.Compiled);
      var fieldRegex = new Regex(@"X=([-.\d]+)|Y=([-.\d]+)|Z=([-.\d]+)|RZ=([-.\d]+)|RY=([-.\d]+)|RX=([-.\d]+)|ROTX=DC\(([-.\d]+)\)");

      var sb = new StringBuilder();
      sb.AppendLine("Transit\tN\tG1\tX\tY\tZ\tRZ\tRY\tRX\tROTX\teRX\teRY\teRZ");


      bool inTransit = false;
      bool afterSkipExit = false;
      string beginTransit = "; Begin transit";
      string endTransit = "; End transit";
      string skipExit = "SKIP_EXIT";
      int nTransit = 1;
      bool capturedStartPosition = false;
      cPose initialPose = new cPose();
      double initialU = 0;
      cPose lastPose = new cPose();
      double lastU = 0;
      string lastLine = string.Empty;


      foreach (var line in lines)
      {
        bool continueToNext = false;
        if (line.Contains(beginTransit))
        {
          inTransit = true;
        }
        if (inTransit && line.Contains(skipExit))
        {
          afterSkipExit = true;
        }

        if (line.Contains(endTransit))
        {
          inTransit = false;
          afterSkipExit = false;
          capturedStartPosition = false;
          nTransit++;
        }

        if (!(inTransit && afterSkipExit))
          continueToNext = true;

        if (!g1Regex.IsMatch(line))
          continueToNext = true;

        if (continueToNext)
        {
          outputLines.Add(line);
          continue;
        }

        // Extract N number from start of line
        int nValue = 0;
        var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && parts[0].StartsWith("N") && int.TryParse(parts[0].Substring(1), out int parsedN))
        {
          nValue = parsedN;
        }

        // Extract field values
        var matches = fieldRegex.Matches(line);
        double[] values = new double[7]; // X, Y, Z, RZ, RY, RX, ROTX



        foreach (Match m in matches)
        {
          for (int i = 1; i <= 7; i++)
          {
            if (m.Groups[i].Success)
            {
              values[i - 1] = double.Parse(m.Groups[i].Value);
              break;
            }
          }
        }

        // Now create the cPose from parsed values
        cPose pose = new cPose
        {
          x = values[0],
          y = values[1],
          z = values[2],
          rz = values[3],
          ry = values[4],
          rx = values[5]
        };

        double Radius = Math.Sqrt(pose.y * pose.y + pose.z * pose.z);
        if (Radius > 195 && !capturedStartPosition) //we're at the safe radius, mark for replacement. 
        {
          initialPose = pose;
          initialU = values[6];
          capturedStartPosition = true;
          outputLines.Add(line);
          continue; // Skip the rest of the processing for this line
        }
        if (!capturedStartPosition)
        {
          // If we are not captured yet, we skip this line
          outputLines.Add(line);
          continue;
        }
        else
        {
          //outputLines.Add(" ; " + line); // Add the original line to output
        }
        if (capturedStartPosition && Radius < 195)
        {
          cLHT initalLHT = new cLHT();
          initalLHT.setTransformFromEulerXYZ(initialPose);
          cPose eXYZinitialPose = initalLHT.getPoseEulerXYZ();

          cTransform rU = new cTransform(0, 0, 0, -initialU, 0, 0);
          cPose xInitialPose = (rU * eXYZinitialPose.getTransform()).getPose();
          //cPose xInitialPose = (eXYZinitialPose.getTransform() * rU).getPose();

          cTransform testU = new cTransform(0, 0, 0, initialU, 0, 0);
          cPose testPose = (testU * xInitialPose.getTransform()).getPose();

          double dx = testPose.x - initialPose.x;
          double dy = testPose.y - initialPose.y;
          double dz = testPose.z - initialPose.z;
          double dRZ = testPose.rz - initialPose.rz;
          double dRY = testPose.ry - initialPose.ry;
          double dRX = testPose.rx - initialPose.rx;
          Console.WriteLine($"Delta: dx={dx:F3} dy={dy:F3} dz={dz:F3} dRZ={dRZ:F3} dRY={dRY:F3} dRX={dRX:F3}".Pastel(Color.Yellow));


          cLHT lastLHT = new cLHT();

          //Adding these to hide the problem with the gCode output, we'll have to adjust YZ to get to the safe radius...Don't Forget!

          lastU = values[6]; // Update lastU to the current value
          lastLHT.setTransformFromEulerXYZ(lastPose);
          cPose eXYZlastPose = lastLHT.getPoseEulerXYZ();

          cTransform rUend = new cTransform(0, 0, 0, -lastU, 0, 0);
          cPose xlastPose = (rUend * eXYZlastPose.getTransform()).getPose();
          //cPose xlastPose = (eXYZlastPose.getTransform() * rUend).getPose();
          lastPose = pose; // Update lastPose to the current pose



          Console.WriteLine("========================================".Pastel(Color.Yellow));
          Console.WriteLine($"Transit: {nTransit} N: {nValue}".Pastel(Color.Yellow));
          Console.WriteLine($"Initial Pose: {initialU:F3} ".Pastel(Color.Yellow));
          Console.WriteLine($"x: {initialPose.x:F3} y: {initialPose.y:F3} z: {initialPose.z:F3} rx: {initialPose.rx:F3} ry: {initialPose.ry:F3} rz: {initialPose.rz:F3} - initialpose".Pastel(Color.Yellow));
          Console.WriteLine($"x: {eXYZinitialPose.x:F3} y: {eXYZinitialPose.y:F3} z: {eXYZinitialPose.z:F3} rx: {eXYZinitialPose.rx:F3} ry: {eXYZinitialPose.ry:F3} rz: {eXYZinitialPose.rz:F3} - eXYZinitialPose".Pastel(Color.Yellow));
          Console.WriteLine($"x: {xInitialPose.x:F3} y: {xInitialPose.y:F3} z: {xInitialPose.z:F3} rx: {xInitialPose.rx:F3} ry: {xInitialPose.ry:F3} rz: {xInitialPose.rz:F3} - xInitalPose (if U = 0)".Pastel(Color.Yellow));

          Console.WriteLine();
          Console.WriteLine($"Last Pose: {lastU:F3} ".Pastel(Color.Yellow));
          Console.WriteLine($"x: {lastPose.x:F3} y: {lastPose.y:F3} z: {lastPose.z:F3} rx: {lastPose.rx:F3} ry: {lastPose.ry:F3} rz: {lastPose.rz:F3} - lastPose".Pastel(Color.Yellow));
          Console.WriteLine($"x: {eXYZlastPose.x:F3} y: {eXYZlastPose.y:F3} z: {eXYZlastPose.z:F3} rx: {eXYZlastPose.rx:F3} ry: {eXYZlastPose.ry:F3} rz: {eXYZlastPose.rz:F3} - eXYZlastPose".Pastel(Color.Yellow));
          Console.WriteLine($"x: {xlastPose.x:F3} y: {xlastPose.y:F3} z: {xlastPose.z:F3} rx: {xlastPose.rx:F3} ry: {xlastPose.ry:F3} rz: {xlastPose.rz:F3} - xlastPose (if U = 0)".Pastel(Color.Yellow));
          Console.WriteLine("========================================".Pastel(Color.Yellow));
          Console.WriteLine();

          // calclate the path:
          var (poses, angles) = GeneratePath(xInitialPose, xlastPose, initialU, lastU, 20);

          //var (poses2, angles2) = GeneratePath(xInitialPose.getLHT(), xlastPose.getLHT(), initialU, lastU, 20);

          //poses = poses2;
          //angles = angles2;

          // output to the Console for review
          Console.WriteLine("Generated Path:".Pastel(Color.Cyan));
          for (int i = 0; i < poses.Count; i++)
          {
            //poses[i].rY = 5;
            //Console.WriteLine($"Pose {i + 1}: X={poses[i].x:F3} Y={poses[i].y:F3} Z={poses[i].z:F3} RX={poses[i].rx:F3} RY={poses[i].ry:F3} RZ={poses[i].rz:F3} Angle={angles[i]:F3}".Pastel(Color.Cyan));
            Console.WriteLine($"X={poses[i].x:F3} Y={poses[i].y:F3} Z={poses[i].z:F3} RZ={poses[i].rz:F3} RY={poses[i].ry:F3} RX={poses[i].rx:F3} ROTX=DC(0)".Pastel(Color.Cyan));
          }

          // transform the poses by the angles
          List<cPose> transformedPoses = new List<cPose>();
          for (int i = 0; i < poses.Count; i++)
          {
            cTransform rUPath = new cTransform(0, 0, 0, -angles[i], 0, 0);
            rUPath = !rUPath;

            cLHT result = rUPath.getLHT() * poses[i].getLHT();

            cPose poseResult = result.getPoseFixedZYX();

            //cPose transformedPose = (rUPath * poses[i].getTransform()).getPose();

            //cPose fixedZYX = (rUPath * poses[i].getTransform()).getLHT().getPoseFixedZYX();

            //cPose transformedPose = (poses[i].getTransform() * rUPath).getPose();

            cPose transformedPose = poseResult;

            transformedPoses.Add(transformedPose);
          }

          // output the transformed poses
          Console.WriteLine("Transformed Path:".Pastel(Color.Magenta));
          for (int i = 0; i < transformedPoses.Count; i++)
          {
            //Console.WriteLine($"Transformed Pose {i + 1}: X={transformedPoses[i].x:F3} Y={transformedPoses[i].y:F3} Z={transformedPoses[i].z:F3} RX={transformedPoses[i].rx:F3} RY={transformedPoses[i].ry:F3} RZ={transformedPoses[i].rz:F3}".Pastel(Color.Magenta));
            Console.WriteLine($"X={transformedPoses[i].x:F3} Y={transformedPoses[i].y:F3} Z={transformedPoses[i].z:F3} RZ={transformedPoses[i].rz:F3} RY={transformedPoses[i].ry:F3} RX={transformedPoses[i].rx:F3} ROTX=DC({angles[i]:F3})".Pastel(Color.Magenta));
          }

          // Add the original line to output
          outputLines.RemoveAt(outputLines.Count - 1); // Remove the last line which is the original G1 line and was commented out. 
          outputLines.RemoveAt(outputLines.Count - 1); // Remove the last line which is the original G1 line and was commented out.

          //output the transformed poses as G1 commands
          for (int i = 0; i < transformedPoses.Count; i++)
          {
            cPose xpose = transformedPoses[i]; // Ensure we are using the Euler ZYX pose
            double angle = angles[i];
            string gcodeLine = $"G1 X={xpose.x:F3} Y={xpose.y:F3} Z={xpose.z:F3} RZ={xpose.rz:F3} RY={xpose.ry:F3} RX={xpose.rx:F3} ROTX=DC({angle:F3}) F=6000.0 ; new pose {i + 1}";
            Console.WriteLine(gcodeLine.Pastel(Color.Green));
            outputLines.Add(gcodeLine);
          }

          //outputLines.Add(lastLine);
          outputLines.Add(line);
          continue; // Skip the rest of the processing for this line
        }
        lastPose = pose;
        lastU = values[6];
        lastLine = line; // Store the last line for output

        // cLHT lht = new cLHT();
        // lht.setTransformFromEulerZYX(pose);
        // cPose poseEulerXYZ = lht.getPoseEulerXYZ();

        //sb.AppendLine($"{nTransit}\t{nValue}\tG1\t{values[0]:0.000}\t{values[1]:0.000}\t{values[2]:0.000}\t{values[3]:0.000}\t{values[4]:0.000}\t{values[5]:0.000}\t{values[6]:0.000}\t{poseEulerXYZ.rx:0.000}\t{poseEulerXYZ.ry:0.000}\t{poseEulerXYZ.rz:0.000}");
      }

      if (sb.Length > 0)
      {
        Clipboard.SetText(sb.ToString());
        Console.WriteLine("Parsed G1 values copied to clipboard.".Pastel(Color.Green));
        Console.WriteLine($"There were {nTransit - 1} detected.".Pastel(Color.Green));
      }
      else
      {
        Console.WriteLine("No G1 lines found.".Pastel(Color.OrangeRed));
      }

      //output the outputLines
      if (outputLines.Count > 0)
      {
        File.WriteAllLines(outputFileName, outputLines);
        Console.WriteLine($"Output written to {outputFileName}".Pastel(Color.Green));
      }
      else
      {
        Console.WriteLine("No output lines generated.".Pastel(Color.OrangeRed));
      }
    }

    public (List<cPose> poses, List<double> angles) GeneratePath(cPose startPose, cPose endPose, double startAngle, double endAngle, int count)
    {
      var poses = new List<cPose>();
      var angles = new List<double>();

      for (int i = 0; i < count; i++)
      {
        double t = (double)i / (count - 1); // Normalized [0,1]

        double x = Lerp(startPose.x, endPose.x, t);
        double y = Lerp(startPose.y, endPose.y, t);
        double z = Lerp(startPose.z, endPose.z, t);
        double rx = Lerp(startPose.rx, endPose.rx, t);
        double ry = Lerp(startPose.ry, endPose.ry, t);
        double rz = Lerp(startPose.rz, endPose.rz, t);

        double angle = Lerp(startAngle, endAngle, t);

        poses.Add(new cPose(x, y, z, rx, ry, rz));
        angles.Add(angle);
      }

      return (poses, angles);
    }

    private double Lerp(double a, double b, double t)
    {
      return a + (b - a) * t;
    }

    public (List<cPose> poses, List<double> angles) GeneratePath(cLHT start, cLHT end, double startAngle, double endAngle, int count)
    {
      var poses = new List<cPose>();
      var angles = new List<double>();

      // Extract rotation and position
      var qStart = new Quaternion(start);
      var qEnd = new Quaternion(end);

      var tStart = new cVector3d((float)start.M[0, 3], (float)start.M[1, 3], (float)start.M[2, 3]);
      var tEnd = new cVector3d((float)end.M[0, 3], (float)end.M[1, 3], (float)end.M[2, 3]);

      for (int i = 0; i < count; i++)
      {
        double t = (double)i / (count - 1);

        // Interpolate translation and rotation
        var interpT = cVector3d.Lerp(tStart, tEnd, (float)t);
        var interpQ = Quaternion.Slerp(qStart, qEnd, (float)t);
        var rotMatrix = interpQ.GetMatrix();

        // Build interpolated LHT
        var lht = new cLHT();
        for (int row = 0; row < 3; row++)
        {
          for (int col = 0; col < 3; col++)
            lht.M[row, col] = rotMatrix[row, col];

          // Use interpolated translation here
          lht.M[row, 3] = interpT.v[row];
        }

        // Set homogeneous bottom row
        lht.M[3, 0] = 0.0;
        lht.M[3, 1] = 0.0;
        lht.M[3, 2] = 0.0;
        lht.M[3, 3] = 1.0;

        poses.Add(lht.getPoseEulerZYX());
        angles.Add(startAngle + (endAngle - startAngle) * t);
      }

      return (poses, angles);
    }

    private void btn_TestTransforms(object sender, EventArgs e)
    {
      cPose poseStart = new cPose(-250, 0, 190, 10, -10, 90);
      cPose poseEnd = new cPose(-1000, 0, 190, -5, 10, -90);
      double startROTX = -90;
      double endROTX = 90;

      cLHT rU = new cLHT(0, 0, 0, startROTX.D2R(), 0, 0);
      poseStart = (rU * poseStart.getLHT()).getPoseEulerXYZ();
      rU = new cLHT(0, 0, 0, endROTX.D2R(), 0, 0);
      poseEnd = (rU * poseEnd.getLHT()).getPoseEulerXYZ();

      double SafeRadius = 195; // Safe radius for the robot

      bool testXYZ = true; // Set to true for testing XYZ, false for ZYX

      if (System.Windows.Forms.MessageBox.Show("Load from GCode lines?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
      {
        string lineStart = "N72192 G1 X=-503.63757 Y=89.10178 Z=-174.57649 RZ=-87.3561474 RY=8.9415109 RX=179.1859510 ROTX=DC(-171.10496)";
        string lineEnd = "N72262 G1 X=-347.04906 Y=112.85317 Z=160.25055 RZ=92.7791606 RY=0.0000000 RX=0.0000000 ROTX=DC(-20.00000)";
        var (startPose, endPose, startU, endU) = ParseGCodeLine(lineStart, lineEnd);
        poseStart = startPose;
        poseEnd = endPose;
        startROTX = startU;
        endROTX = endU;
      }

      int count = (int)(endROTX - startROTX);

      List<string> SafeMoves = ConvertProgram.insertSafeMove(poseStart, startROTX);
      var (poses, angles) = GeneratePath(poseStart, poseEnd, startROTX, endROTX, count);
      cTransform rUStart = new cTransform(0, 0, 0, startROTX, 0, 0);
      cPose poseStartZYX = (rUStart.getLHT() * poseStart.getLHT()).getPoseEulerZYX();

      int nNum = 1;
      Console.WriteLine($"N{nNum++} F1000");
      //Console.WriteLine($"N{nNum++} ORIVIRT1; USE 21120 CONFIGURED AS EulerZYX".Pastel(Color.Yellow));
      if (!testXYZ)
      {
        foreach (var move in SafeMoves)
        {
          Console.WriteLine($"N{nNum++} {move}".Pastel(Color.Yellow));
        }

        //Console.WriteLine($"N{nNum++} APPROACH_ROTX({poseStartZYX.x:F3}, {poseStartZYX.y:F3}, {poseStartZYX.z:F3}, {poseStartZYX.rz:F3}, {poseStartZYX.ry:F3}, {poseStartZYX.rx:F3}, {startROTX:F3}, 48161.141);".Pastel(Color.Green));
        //Console.WriteLine($"N{nNum++} ORIVIRT2; USE 21130 CONFIGURED AS EulerXYZ".Pastel(Color.Yellow));
      }
      else
      {
        foreach (var move in SafeMoves)
        {
          Console.WriteLine($"N{nNum++} {move}".Pastel(Color.Yellow));
        }
        // Console.WriteLine($"N{nNum++} STOPRE".Pastel(Color.Yellow));
        // Console.WriteLine($"N{nNum++} TRAORI".Pastel(Color.Yellow));
        // Console.WriteLine($"N{nNum++} ROTX_TRAFO(1)".Pastel(Color.Green));
        // Console.WriteLine($"N{nNum++} STOPRE".Pastel(Color.Yellow));
      }

      Console.WriteLine($"N{nNum++} M0");
      for (int i = 0; i < poses.Count; i++)
      {

        cTransform ROTX = new cTransform(0, 0, 0, angles[i], 0, 0);
        cPose poseZYX;

        if (testXYZ)
        {
          poseZYX = (ROTX.getLHT() * poses[i].getLHT()).getPoseEulerXYZ();
          poseZYX = poses[i];
        }
        else
        {
          poseZYX = (ROTX.getLHT() * poses[i].getLHT()).getPoseEulerZYX();
        }

        if (Math.Abs(Math.Abs(poseZYX.rY) - 90) < 10)
        {
          continue; // Skip poses where RY is close to 90 degrees
        }

        // Adjust the poseZYX to ensure it is at the safe radius
        double radius = Math.Sqrt(poseZYX.y * poseZYX.y + poseZYX.z * poseZYX.z);
        if (radius < SafeRadius)
        {
          double scale = SafeRadius / radius;
          poseZYX.y *= scale;
          poseZYX.z *= scale;
        }

        string poseString;
        if (testXYZ)
          poseString = $"X={poseZYX.x:F3} Y={poseZYX.y:F3} Z={poseZYX.z:F3} RZ={poseZYX.rx:F3} RY={poseZYX.ry:F3} RX={poseZYX.rz:F3} ROTX=DC({angles[i].m180p180():F3})";
        else
          poseString = $"X={poseZYX.x:F3} Y={poseZYX.y:F3} Z={poseZYX.z:F3} RZ={poseZYX.rz:F3} RY={poseZYX.ry:F3} RX={poseZYX.rx:F3} ROTX=DC({angles[i].m180p180():F3})";


        if (i == 0)
        {

          Console.WriteLine($"N{nNum++} {poseString}".Pastel(Color.Cyan));
          Console.WriteLine($"N{nNum++} M0");
        }
        Console.WriteLine($"N{nNum++} {poseString}".Pastel(Color.Green));
      }



      Console.WriteLine($"N{nNum++} M0 ; Wait for user to continue".Pastel(Color.Yellow));
      Console.WriteLine($"N{nNum++} ORIVIRT1; USE 21120 CONFIGURED AS EulerZYX".Pastel(Color.Yellow));
      Console.WriteLine($"N{nNum++} M2 ; End of program".Pastel(Color.Yellow));

    }

    private (cPose startPose, cPose endPose, double startU, double endU) ParseGCodeLine(string lineStart, string lineEnd)
    {
      var startMatch = Regex.Match(lineStart, @"X=([-.\d]+) Y=([-.\d]+) Z=([-.\d]+) RZ=([-.\d]+) RY=([-.\d]+) RX=([-.\d]+) ROTX=DC\(([-.\d]+)\)");
      var endMatch = Regex.Match(lineEnd, @"X=([-.\d]+) Y=([-.\d]+) Z=([-.\d]+) RZ=([-.\d]+) RY=([-.\d]+) RX=([-.\d]+) ROTX=DC\(([-.\d]+)\)");

      if (!startMatch.Success || !endMatch.Success)
      {
        throw new Exception("Failed to parse start or end line.");
      }

      // Parse start values
      double startX = double.Parse(startMatch.Groups[1].Value);
      double startY = double.Parse(startMatch.Groups[2].Value);
      double startZ = double.Parse(startMatch.Groups[3].Value);
      double startRZ = double.Parse(startMatch.Groups[4].Value);
      double startRY = double.Parse(startMatch.Groups[5].Value);
      double startRX = double.Parse(startMatch.Groups[6].Value);
      double startU = double.Parse(startMatch.Groups[7].Value);

      cLHT startLHT = new cLHT();
      startLHT.setTransformFromEulerZYX(startX, startY, startZ, startRZ, startRY, startRX);
      cPose startPose = startLHT.getPoseEulerXYZ();
      Console.WriteLine($";Start Pose before xForm: X={startPose.x:F3} Y={startPose.y:F3} Z={startPose.z:F3} RX={startPose.rx:F3} RY={startPose.ry:F3} RZ={startPose.rz:F3} ROTX=DC({startU:F3})".Pastel(Color.Cyan));

      cTransform rUStart = new cTransform(0, 0, 0, -startU, 0, 0);
      //startPose = (rUStart.getLHT() * startPose.getLHT()).getPoseEulerXYZ();

      // Parse end values
      double endX = double.Parse(endMatch.Groups[1].Value);
      double endY = double.Parse(endMatch.Groups[2].Value);
      double endZ = double.Parse(endMatch.Groups[3].Value);
      double endRZ = double.Parse(endMatch.Groups[4].Value);
      double endRY = double.Parse(endMatch.Groups[5].Value);
      double endRX = double.Parse(endMatch.Groups[6].Value);
      double endU = double.Parse(endMatch.Groups[7].Value);

      cLHT endLHT = new cLHT();
      endLHT.setTransformFromEulerZYX(endX, endY, endZ, endRZ, endRY, endRX);
      cPose endPose = endLHT.getPoseEulerXYZ();
      cTransform rUEnd = new cTransform(0, 0, 0, -endU, 0, 0);
      Console.WriteLine($";End Pose before xForm: X={endPose.x:F3} Y={endPose.y:F3} Z={endPose.z:F3} RX={endPose.rx:F3} RY={endPose.ry:F3} RZ={endPose.rz:F3} ROTX=DC({endU:F3})".Pastel(Color.Cyan));
      //endPose = (rUEnd.getLHT() * endPose.getLHT()).getPoseEulerXYZ();

      // write startPose and endPose to the console for debugging
      Console.WriteLine($";Start Pose: X={startPose.x:F3} Y={startPose.y:F3} Z={startPose.z:F3} RX={startPose.rx:F3} RY={startPose.ry:F3} RZ={startPose.rz:F3} ROTX=DC({startU:F3})".Pastel(Color.Cyan));
      Console.WriteLine($";End Pose: X={endPose.x:F3} Y={endPose.y:F3} Z={endPose.z:F3} RX={endPose.rx:F3} RY={endPose.ry:F3} RZ={endPose.rz:F3} ROTX=DC({endU:F3})".Pastel(Color.Cyan));


      // Return the parsed poses and U values
      return (startPose, endPose, startU, endU);
    }



    private void btnConvertAProgram_Click(object sender, EventArgs e)
    {

      OpenFileDialog ofd = new OpenFileDialog();
      ofd.Filter = "GCode files (*.mpf)|*.mpf|All files (*.*)|*.*";
      ofd.Title = "Select GCode File";

      if (ofd.ShowDialog() != DialogResult.OK)
      {
        MessageBox.Show("No file selected.");
        return;
      }

      // Write the output lines to a new file
      string directory = Path.GetDirectoryName(ofd.FileName);
      string filenameWithoutExt = Path.GetFileNameWithoutExtension(ofd.FileName);
      string outputFileName = Path.Combine(directory, filenameWithoutExt + "_twr.mpf");

      ConvertProgram.ConvertAProgram(ofd.FileName, outputFileName);


    }

    private void button5_Click(object sender, EventArgs e)
    {
      OpenFileDialog ofd = new OpenFileDialog();
      ofd.Filter = "GCode files (*.mpf)|*.mpf|All files (*.*)|*.*";
      ofd.Title = "Select GCode File";

      if (ofd.ShowDialog() != DialogResult.OK)
      {
        MessageBox.Show("No file selected.");
        return;
      }

      string[] lines = File.ReadAllLines(ofd.FileName);
      //foreach (var line in lines)
      //{
      //  if (line.Contains("APPROACH_ROTX_V2"))
      //  {
      //    Console.WriteLine(line.Pastel(Color.Green));
      //  }
      //}

      var seenROTX = new HashSet<string>(); // Using string to avoid float precision fuzz
      foreach (var line in lines)
      {
        if (!line.Contains("APPROACH_ROTX_V2(")) continue;

        var start = line.IndexOf('(');
        var end = line.IndexOf(')');
        if (start < 0 || end < 0) continue;

        var args = line.Substring(start + 1, end - start - 1).Split(',');

        if (args.Length < 7) continue;

        var rotxRaw = args[6].Trim();
        // Round or normalize to 3 decimals to group near-matches
        if (double.TryParse(rotxRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out double rotx))
        {
          var key = rotx.ToString("F3"); // Format to 3 decimal places
          if (seenROTX.Add(key))
          {
            Console.WriteLine(line); // Unique ROTX value
          }
        }
      }


    }

    private void btnSparCorner_Click(object sender, EventArgs e)
    {
      OpenFileDialog ofd = new OpenFileDialog();
      ofd.Filter = "GCode files (*.mpf)|*.mpf|All files (*.*)|*.*";
      ofd.Title = "Select GCode File";

      if (ofd.ShowDialog() != DialogResult.OK)
      {
        MessageBox.Show("No file selected.");
        return;
      }

      // Write the output lines to a new file
      string directory = Path.GetDirectoryName(ofd.FileName);
      string filenameWithoutExt = Path.GetFileNameWithoutExtension(ofd.FileName);
      string outputFileName = Path.Combine(directory, filenameWithoutExt + "_spar.mpf");

      ConvertProgram.sparTreatment(ofd.FileName, outputFileName);
    }
  }
}
