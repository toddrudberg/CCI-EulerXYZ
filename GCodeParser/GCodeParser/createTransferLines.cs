using MathNet.Numerics.Providers.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GCodeParser
{
  internal class createTransferLines
  {
    public createTransferLines()
    {
     
    }

    // public static void InsertTransitKeys(List<string> lines, out List<int> startKeys, out List<int> endKeys)
    // {
    //   bool inACourse = false;
    //   bool cutFound = false;
    //   bool inTransit = false;
    //   startKeys = new List<int>();
    //   endKeys = new List<int>();
    //   for (int i = 0; i < lines.Count; i++)
    //   {
    //     string line = lines[i];
    //     if (line.Contains("COURSE"))
    //     {
    //       cutFound = true;
    //     }        
    //   }
    // }

    public static List<string> massageTransferLines(List<string> lines)
    {
      List<string> result = new List<string>();
      result = lines.ToList();
      List<int> startTransits = new List<int>();
      List<int> endTransits = new List<int>();

      string beginTransit = "; Begin transit";
      string endTransit = "; End transit";

      for (int i = 0; i < lines.Count; i++)
      {
        if (lines[i].Contains(beginTransit))
        {
          startTransits.Add(i);
        }

        if (lines[i].Contains(endTransit))
        {
          endTransits.Add(i);
        }
      }
      var fieldRegex = new Regex(@"X=([-.\d]+)|Y=([-.\d]+)|Z=([-.\d]+)|RZ=([-.\d]+)|RY=([-.\d]+)|RX=([-.\d]+)|ROTX=DC\(([-.\d]+)\)");

      List<List<string>> newTransits = new List<List<string>>();
      if (startTransits.Count == endTransits.Count)
      {

        for (int i = 0; i < startTransits.Count; i++)
        {
          int startIndex = startTransits[i];
          int endIndex = endTransits[i];
          var range = lines.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();

          cPose startTransitPose = new cPose(); double startU = 0;
          cPose endTransitPose = new cPose(); double endU = 0;

          List<string> sTransitPath = new List<string>();
          bool gotFirstTransitLine = false;
          bool gotSkipExit = false;
          cPose previousPose = new cPose();
          int firstTransitLine = 0;
          for (int j = 0; j < range.Count; j++)
          {
            string line = range[j];

            if (!gotSkipExit)
            {
              if (line.Contains("SKIP_EXIT"))
              {
                gotSkipExit = true;
              }
              continue;
            }

            cPose Pose = new cPose();
            double Uarg;
            if (line.Contains("G1") || line.Contains("G9"))
            {
              ConvertProgram.getMotionArguments(fieldRegex, line, out Uarg, out Pose, true);

              if (!gotFirstTransitLine)
              {
                if (Math.Sqrt(Pose.Y * Pose.Y + Pose.Z * Pose.Z) > 195)
                {
                  startU = Uarg;
                  startTransitPose = Pose;
                  firstTransitLine = j;
                  gotFirstTransitLine = true;
                }
                continue;
              }
              if (Math.Sqrt(Pose.Y * Pose.Y + Pose.Z * Pose.Z) < 195)
              {
                //last move of transit
                endU = Uarg;
                endTransitPose = previousPose;
                range.RemoveAt(j);
                var transitPath = ConvertProgram.GeneratePath(startTransitPose, endTransitPose, startU, Uarg, (int)Math.Abs(endU - startU) / 5); //use the last Uarg because of the hiccup. 
                for (int k = 0; k < transitPath.angles.Count; k++)
                {
                  cPose transitPose = transitPath.poses[k];
                  double uArg = transitPath.angles[k];
                  string stransit = $"X={transitPose.X:0.000} Y={transitPose.Y:0.000} Z={transitPose.Z:0.000} RZ={transitPose.rX:0.000} RY={transitPose.rY:0.000} RX={transitPose.rZ:0.000} ROTX=DC({uArg:0.000})";

                  sTransitPath.Add(stransit);
                }
                int nItemsToRemove = j - 1 - firstTransitLine + 1;
                range.RemoveRange(firstTransitLine, nItemsToRemove);
                range.InsertRange(j - nItemsToRemove - 1, sTransitPath);

                newTransits.Add(range);

                break;
              }
              previousPose = Pose;
            }
          }
        }

        for (int i = newTransits.Count - 1; i >= 0; i--)
        {
          result.RemoveRange(startTransits[i], endTransits[i] - startTransits[i] + 1);
          result.InsertRange(startTransits[i], newTransits[i]);
        }
      }
      return result;
    }
  }
}
