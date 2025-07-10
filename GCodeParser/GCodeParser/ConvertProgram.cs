using Electroimpact.FileParser;
using Pastel;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;

namespace GCodeParser
{
  internal class ConvertProgram
  {

    public static List<string> cleanUpBasics(List<string> lines)
    {
      List<string> result = new List<string>();
      Electroimpact.FileParser.cFileParse fp = new Electroimpact.FileParser.cFileParse();

      int processNum = 0;
      int courseNum = -1;
      double lastDist = 0;
      double darg = 0;
      foreach (string line in lines)
      {
        switch (processNum)
        {
          case 0:  // wait for course number

            result.Add(line);
            if (line.Contains("SKIP_RESTART") && fp.TryGetArgument(line, "COURSE_NUM=", ref darg))
            {
              courseNum = (int)darg;
              //Console.WriteLine($"Found course number: {courseNum}");
              processNum++;
            }
            break;
          case 1: // wait for DIST= argument - you're at the start of a course
            result.Add(line);
            if (fp.TryGetArgument(line, "DIST=", ref darg))
            {
              //Console.WriteLine($"Found DIST= argument: {darg}");
              lastDist = darg;
              processNum++;
            }
            break;
          case 2: // wait for cut command, you're nearing the end of a course.
            if (line.Contains("cut"))
            {
              //Console.WriteLine($"Found cut command in course {courseNum}.");
              result.Add(line);
              processNum++;
            }
            else
            {
              CheckIfMissingDISTCommand(line, result, ref lastDist);
              
            }
            break;
          case 3: // wait for G9 to indicate last move of the course. you've reached the end of this course.
            if (line.Contains("G9"))
            {
              //Console.WriteLine($"Found G9 command in course {courseNum}.");
              result.Add(line);
              processNum = 0;
            }
            else
            {
              CheckIfMissingDISTCommand(line, result, ref lastDist);
            }
            break;
        }        
      }
      return result;
    }

    private static void CheckIfMissingDISTCommand(string line, List<string> result, ref double lastDist)
    {
      double darg = 0;
      Electroimpact.FileParser.cFileParse fp = new Electroimpact.FileParser.cFileParse();
      if (fp.TryGetArgument(line, "G", ref darg))
      {
        if ((int)darg == 1 || (int)darg == 9)
        {
          if (line.Contains("DIST="))
          {
            fp.TryGetArgument(line, "DIST=", ref darg);
            if (darg - lastDist > 4.0)
            { 
              lastDist = darg;
              result.Add(line);
            }
          }
        }
        else
        {
          result.Add(line);
        }
      }
      else
      {
        result.Add(line);
      }

      return;
    }

    public static void ConvertAProgram(string inputFile, string outputFile)
    {
      Console.WriteLine($"Converting {inputFile} to {outputFile} in rXrYrZ format.");

      string[] lines = File.ReadAllLines(inputFile);

      lines = cleanUpBasics(lines.ToList()).ToArray();


      var g1Regex = new Regex(@"G1\s+.*", RegexOptions.Compiled);
      var g9Regex = new Regex(@"G9\s+.*", RegexOptions.Compiled);

      var fieldRegex = new Regex(@"X=([-.\d]+)|Y=([-.\d]+)|Z=([-.\d]+)|RZ=([-.\d]+)|RY=([-.\d]+)|RX=([-.\d]+)|ROTX=DC\(([-.\d]+)\)");

      List<string> outputLines = new List<string>();
      for (int nline = 0; nline < lines.Length; nline++)
      {
        string line = lines[nline];
        if (line.Contains("APPROACH_ROTX"))
        {
          var safeStartEnd = ProcessSafeMoveCommand(line);

          //Uncomment to use our safemove.  Use insertEulerXYZCodes, to use their safe move and afterwords set ORIYP2
          //List<string> SafeMoves = insertSafeMove(safeStartEnd.Target, safeStartEnd.TargetU);
          List<string> SafeMoves = insertEulerXYZCodes(line);
          for (int i = 0; i < SafeMoves.Count; i++)
          {
            outputLines.Add(SafeMoves[i]);
          }
          //for (int nLookAhead = nline + 1; nLookAhead < lines.Length; nLookAhead++)
          //{
          //  if (g1Regex.IsMatch(lines[nLookAhead]))
          //  {
          //    double Uarg;
          //    cPose endpose;
          //    getMotionArguments(fieldRegex, lines[nLookAhead], out Uarg, out endpose);
          //    cLHT endlhtPose = new cLHT();
          //    endlhtPose.setTransformFromEulerZYX(endpose);
          //    cLHT Ucmd = new cLHT(0, 0, 0, -Uarg.D2R(), 0, 0);
          //    cPose endcartesianPose = (Ucmd * endlhtPose).getPoseEulerXYZ();
          //    // now drop the safe move lines

          //    //this is broken.  We are expecting Euler rx ry rz, with the rotation built in.  then we undo the rotation in GeneratePath, generate the path, and then apply the rotation again.
          //    var (poses, angles) = GeneratePath(safeStartEnd.Target, endpose, safeStartEnd.TargetU, Uarg, (int)(Uarg - safeStartEnd.TargetU));
              
          //    for (int i = 0; i < poses.Count; i++)
          //    {
          //      cTransform ROTX = new cTransform(0, 0, 0, angles[i], 0, 0);
          //      cPose poseZYX;

          //      poseZYX = (ROTX.getLHT() * poses[i].getLHT()).getPoseEulerXYZ();
          //      string poseString = $"X={poseZYX.x:F3} Y={poseZYX.y:F3} Z={poseZYX.z:F3} RZ={poseZYX.rx:F3} RY={poseZYX.ry:F3} RX={poseZYX.rz:F3} ROTX=DC({angles[i].m180p180():F3})";
          //      outputLines.Add(poseString);
          //    }

          //    break;
          //  }

          //}


          continue;
        }
        bool isG1 = g1Regex.IsMatch(line);
        bool isG9 = g9Regex.IsMatch(line);
        if (isG9)
        {
          //Console.WriteLine("got g9");
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

        cPose pose;
        double W;
        getMotionArguments(fieldRegex, line, out W, out pose);
        Electroimpact.FileParser.cFileParse fp = new Electroimpact.FileParser.cFileParse();
        double distArg;
        bool gotDistArg = fp.GetArgument(line, "DIST=", out distArg, true);
        string distString = gotDistArg ? $"DIST={distArg:F3}" : "";
        //Console.WriteLine($"DIST={distArg:0.000}");
        cLHT lhtPose = new cLHT();
        lhtPose.setTransformFromEulerZYX(pose);
        cLHT Wcmd = new cLHT(0, 0, 0, -W.D2R(), 0, 0);
        //cPose cartesianPose = (Wcmd * lhtPose).getPoseEulerXYZ();
        cPose cartesianPose = lhtPose.getPoseEulerXYZ();
        //Console.WriteLine($"X: {cartesianPose.X:0.000} Y: {cartesianPose.Y:0.000} Z: {cartesianPose.Z:0.000} RZ: {cartesianPose.rX:0.000} RY: {cartesianPose.rY:0.000} RX: {cartesianPose.rZ:0.000} ROTX: {W:0.000}".Pastel(Color.Green));

        // Create the output line        
        string outputLineType = isG1 ? "G1" : "G9";
        string outputLine = $"N{nValue} {outputLineType} X={cartesianPose.X:0.000} Y={cartesianPose.Y:0.000} Z={cartesianPose.Z:0.000} RZ={cartesianPose.rX:0.000} RY={cartesianPose.rY:0.000} RX={cartesianPose.rZ:0.000} ROTX=DC({W:0.000}) {distString}";
        outputLines.Add(outputLine);
      }

      //outputLines = createTransferLines.massageTransferLines(outputLines);


      if (outputLines.Count > 0)
      {
        File.WriteAllLines(outputFile, outputLines);
        Console.WriteLine($"Parsed G1 values written to {outputFile}".Pastel(Color.Green));
      }
      else
      {
        Console.WriteLine("No G1 lines found.".Pastel(Color.OrangeRed));
      }
    }

    public static void getMotionArguments(Regex fieldRegex, string line, out double uArg, out cPose pose, bool orderIsXYZ = false)
    {
      // Extract field values
      var matches = fieldRegex.Matches(line);
      double[] values = new double[7];
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

      

      pose = new cPose
      {
        x = values[0],
        y = values[1],
        z = values[2],
        rz = values[3],
        ry = values[4],
        rx = values[5]
      };

      if( orderIsXYZ)
      {
        double temp = pose.rx;
        pose.rx = pose.rz;
        pose.rz = temp;
      }

      cLHT setLHT = new cLHT();
      //setLHT.setTransformFromEulerZYX(pose.x, pose.y, pose.z, pose.rz, pose.ry, pose.rx);
      //pose = setLHT.getPoseEulerXYZ();
      uArg = values[6];
    }

    public static void sparTreatment(string filein, string fileout, double minSpacing = 1.9)
    {
      Console.WriteLine($"Converting {filein} to {fileout} to insert pauses at inflections.");
      string[] lines = File.ReadAllLines(filein);
      List<string> outputLines = new List<string>();

      int state = 0;
      double distLast = 0;

      bool ShouldCopyLine(string line, ref double distLast)
      {
        if (!line.Contains("G1"))
          return true;

        var fp = new Electroimpact.FileParser.cFileParse();
        if (fp.GetArgument(line, "DIST=", out double darg, true))
        {
          if (darg - distLast > minSpacing)
          {
            distLast = darg;
            return true;
          }
          return false;
        }

        return true;
      }

      foreach (string line in lines)
      {
        bool copyLine = true;

        switch (state)
        {
          case 0:
            if (line.Contains("FEED"))
              state++;
            break;

          case 1:
            if (line.Contains("cut"))
              state++;
            copyLine = ShouldCopyLine(line, ref distLast);
            break;

          case 2:
            if (line.Contains("UV(0)"))
              state = 0;
            copyLine = ShouldCopyLine(line, ref distLast);
            break;
        }

        if (copyLine)
          outputLines.Add(line);
        //else
        //  outputLines.Add("; " + line); // Comment out the line if it doesn't meet the criteria
      }

      File.WriteAllLines(fileout, outputLines);
    }


    public static (cPose Target, double TargetU) ProcessSafeMoveCommand(string line)
    {
      //string line = "N72108 APPROACH_ROTX(-347.91276, 49.30709, -56.66891, -87.3568472, 8.9901902, 179.1869604, -171.05626, 48161.141)";
      string pattern = @"APPROACH_ROTX\(([-\d\.]+), ([-\d\.]+), ([-\d\.]+), ([-\d\.]+), ([-\d\.]+), ([-\d\.]+), ([-\d\.]+), ([-\d\.]+)\)";

      var match = Regex.Match(line, pattern);
      if (match.Success)
      {
        var numbers = match.Groups.Cast<Group>()
                         .Skip(1) // skip the full match group
                         .Select(g => double.Parse(g.Value))
                         .ToList();

        // Example: print them
        // foreach (var number in numbers)
        //   Console.WriteLine(number);


        double startX = numbers[0];
        double startY = numbers[1];
        double startZ = numbers[2];
        double startRZ = numbers[3];
        double startRY = numbers[4];
        double startRX = numbers[5];

        cLHT startLHT = new cLHT();
        startLHT.setTransformFromEulerZYX(startX, startY, startZ, startRZ, startRY, startRX);
        cPose startPose = startLHT.getPoseEulerXYZ();
        return (startPose, numbers[6]);
      }
      else
      {
        Console.WriteLine("No match found.");
        return (new cPose(), 0);
      }
    }

    public static List<string> insertEulerXYZCodes(string line)
    {
      List<string> result = new List<string>();
      result.Add(line);
      result.Add("ORIRPY2");
      result.Add("M0");
      return result;
    }
    public static List<string> insertSafeMove(cPose Target, double TargetRotX)
    {
      /*
      F10000
      G1
      TRAORI
      ORIRPY2
      ROTX_TRAFO(0)
      ROTX=0
      ROTX_TRAFO(1)
      X=-500
      Y=0
      Z=300
      RX=0
      RY=0
      RZ=0
      ROTX=0
      M0
      RX=10
      M0
      RY=10
      */
      List<string> safeMoveLines = new List<string>();
      safeMoveLines.Add("G1 F1000");
      safeMoveLines.Add("TRAORI");
      safeMoveLines.Add("ORIRPY2");
      safeMoveLines.Add("ROTX_TRAFO(0) ; Set ROTX Trafo to 0");
      safeMoveLines.Add($"ROTX={TargetRotX:F3}");
      safeMoveLines.Add("ROTX_TRAFO(1) ; Set ROTX Trafo to 1");

      cPose startPose = new cPose(-500, 0, 300, 0, 0, 0);
      cLHT rotatorStart = new cLHT(0, 0, 0, TargetRotX.D2R(), 0, 0);
      startPose = (rotatorStart * startPose.getLHT()).getPoseEulerXYZ();



      safeMoveLines.Add($"X={startPose.X:F3} Y={startPose.Y:F3} Z={startPose.Z:F3} RZ={startPose.rX:F3} RY={startPose.rY:F3} RX={startPose.rZ:F3} F=6000.0 ; Safe Move to initial position");
      safeMoveLines.Add("M0 ; Wait for user to continue");
      //cTransform rU = new cTransform(0, 0, 0, TargetRotX, 0, 0);
      //cPose initialPose = (rU.getLHT() * Target.getLHT()).getPoseEulerXYZ();
      //safeMoveLines.Add($"G1 X={initialPose.X:F3} Y={initialPose.Y:F3} Z={initialPose.Z:F3} RZ={initialPose.rX:F3} RY={initialPose.rY:F3} RX={initialPose.rZ:F3} ROTX=DC({TargetRotX:F3}) F=6000.0 ; Safe Move to Target Position");
      safeMoveLines.Add($"G1 X={Target.X:F3} Y={Target.Y:F3} Z={Target.Z:F3} RZ={Target.rX:F3} RY={Target.rY:F3} RX={Target.rZ:F3} ROTX=DC({TargetRotX:F3}) F=6000.0 ; Safe Move to Target Position");
      return safeMoveLines;
    }


    public static (List<cPose> poses, List<double> angles) GeneratePath(cPose startPose, cPose endPose, double startAngle, double endAngle, int count)
    {
      var poses = new List<cPose>();
      var angles = new List<double>();

      // Local Lerp function
      double Lerp(double a, double b, double t)
      {
        return a + (b - a) * t;
      }
      cTransform uStart = new cTransform(0, 0, 0, -startAngle, 0, 0);
      cTransform uEnd = new cTransform(0,0,0, -endAngle, 0, 0);
      startPose = (uStart.getLHT() * startPose.getLHT()).getPoseEulerXYZ();
      endPose = (uEnd.getLHT() * endPose.getLHT()).getPoseEulerXYZ();

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

      for(int i = 0; i < poses.Count; i++)
      {
        cTransform rotx = new cTransform(0, 0, 0, angles[i], 0, 0);
        poses[i] = (rotx.getLHT() * poses[i].getLHT()).getPoseEulerXYZ();
      }

      return (poses, angles);
    }
  }
}
