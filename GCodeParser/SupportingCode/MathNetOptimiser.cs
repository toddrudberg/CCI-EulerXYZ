using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Optimization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KukaKinematics;

internal class MathNetOptimiser
{

    public class FitAnalysis
    {
        public List<double> residuals = new List<double>();
        public double average = 0;
        public double stdev = 0;
        public double rmsError = 0;
        public double a3s = 0;
        public cTransform xBaseTransform = new cTransform(0, 0, 0, 0, 0, 0);
        private double A3SmaxError = 1;

        public FitAnalysis()
        {

        }

        public bool passedFit()
        {
            // if the a3s is greater than 1.0, then we did not pass the fit
            return a3s <= A3SmaxError;
        }

        public double FitCriteria
        {
            set
            {
                A3SmaxError = value;
            }
        }
    }

    public class BoreasFitAnalysis : FitAnalysis
    {
        public cTransform xToolTransform = new cTransform(0, 0, 0, 0, 0, 0);
    }
    // Define the function to minimize: f(x) = (x - 3)^2
    //public static BoreasFitAnalysis SolveBoreasToFRSTransform(List<cCalibrationDataLogger> dataIn)
    //{
    //    List<cPose> boreas = new List<cPose>();
    //    List<cPose> measured = new List<cPose>();
    //    // Initialize transforms
    //    cTransform boreasBase = new cTransform(0, 0, 0, 0, 0, 0);
    //    cTransform boreasTool = new cTransform(0, 0, 0, 180, 0, 90);  //we need a clean way to set this.  This is set inside the boreas and we need to get it out and start with it here. 


    //    for (int i = 0; i < dataIn.Count; i++)
    //    {
    //        cPose b;
    //        cPose t;

    //        cCalibrationDataLogger d = dataIn[i];
    //        b = new cPose(0, 0, 0, d.BoreasMeasured.rX, d.BoreasMeasured.rY, d.BoreasMeasured.rZ);
    //        t = new cPose(0, 0, 0, d.tmacInFRS.rX, d.tmacInFRS.rY, d.tmacInFRS.rZ);

    //        boreas.Add(b);
    //        measured.Add(t);
    //    }

    //    // Objective function
    //    Func<MathNet.Numerics.LinearAlgebra.Vector<double>, double> objectiveFunction = (MathNet.Numerics.LinearAlgebra.Vector<double> x) =>
    //    {
    //        double sumsq = 0;

    //        // Initialize transforms
    //        cTransform xbase = new cTransform(0, 0, 0, x[0].R2D(), x[1].R2D(), x[2].R2D());
    //        cTransform xtool = new cTransform(0, 0, 0, x[3].R2D(), x[4].R2D(), x[5].R2D());

    //        // Ensure lists match in size
    //        if (boreas.Count != measured.Count)
    //            throw new InvalidOperationException("Mismatch between boreas and measured data counts.");

    //        // Compute error sum
    //        for (int i = 0; i < boreas.Count; i++)
    //        {
    //            // Transform boreasXYZ using xbase and xtool
    //            cTransform boreasXYZ = (xbase.getLHT() * boreas[i].getLHT() * xtool.getLHT()).getTransformEulerXYZ();

    //            // Normalize angles to [-180, 180] before subtraction
    //            double normRx = boreasXYZ.rx.m180p180();
    //            double normRy = boreasXYZ.ry.m180p180();
    //            double normRz = boreasXYZ.rz.m180p180();

    //            double deltaRx = (normRx - measured[i].rx).m180p180();
    //            double deltaRy = (normRy - measured[i].ry).m180p180();
    //            double deltaRz = (normRz - measured[i].rz).m180p180();

    //            // Sum of squared errors
    //            sumsq += Math.Pow(deltaRx, 2) + Math.Pow(deltaRy, 2) + Math.Pow(deltaRz, 2);
    //        }
    //        return Math.Sqrt(sumsq);
    //    };

    //    Func<MathNet.Numerics.LinearAlgebra.Vector<double>, double> objectiveFunctionBaseOnly = (MathNet.Numerics.LinearAlgebra.Vector<double> x) =>
    //    {
    //        double sumsq = 0;

    //        cTransform xbase = new cTransform(0, 0, 0, x[0].R2D(), x[1].R2D(), x[2].R2D());
    //        cTransform xtool = boreasTool;

    //        // Ensure lists match in size
    //        if (boreas.Count != measured.Count)
    //            throw new InvalidOperationException("Mismatch between boreas and measured data counts.");

    //        // Compute error sum
    //        for (int i = 0; i < boreas.Count; i++)
    //        {
    //            // Transform boreasXYZ using xbase and xtool
    //            cTransform boreasXYZ = (xbase.getLHT() * boreas[i].getLHT() * xtool.getLHT()).getTransformEulerXYZ();

    //            // Normalize angles to [-180, 180] before subtraction
    //            double normRx = boreasXYZ.rx.m180p180();
    //            double normRy = boreasXYZ.ry.m180p180();
    //            double normRz = boreasXYZ.rz.m180p180();

    //            double deltaRx = (normRx - measured[i].rx).m180p180();
    //            double deltaRy = (normRy - measured[i].ry).m180p180();
    //            double deltaRz = (normRz - measured[i].rz).m180p180();

    //            // Sum of squared errors
    //            sumsq += Math.Pow(deltaRx, 2) + Math.Pow(deltaRy, 2) + Math.Pow(deltaRz, 2);

    //        }

    //        return Math.Sqrt(sumsq);
    //    };

    //    // Numerical gradient calculation
    //    static MathNet.Numerics.LinearAlgebra.Vector<double> NumericalGradient(Func<MathNet.Numerics.LinearAlgebra.Vector<double>, double> objectiveFunction, MathNet.Numerics.LinearAlgebra.Vector<double> parameters, double epsilon = 1e-8)
    //    {
    //        int dim = parameters.Count;
    //        MathNet.Numerics.LinearAlgebra.Vector<double> gradient = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(dim);

    //        for (int j = 0; j < dim; j++)
    //        {
    //            // Clone and perturb parameter
    //            MathNet.Numerics.LinearAlgebra.Vector<double> perturbedParams = parameters.Clone();
    //            perturbedParams[j] += epsilon;

    //            double f1 = objectiveFunction(perturbedParams);
    //            double f0 = objectiveFunction(parameters);

    //            gradient[j] = (f1 - f0) / epsilon;  // Approximate derivative
    //        }

    //        return gradient;
    //    }


    //    // Define initial guess (must match 6 parameters)
    //    var initialGuess = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfArray(new double[] { boreasBase.rx.D2R(), boreasBase.ry.D2R(), boreasBase.rz.D2R(), boreasTool.rx.D2R(), boreasTool.ry.D2R(), boreasTool.rz.D2R() });

    //    // Define the optimization problem
    //    var objective = ObjectiveFunction.Gradient(objectiveFunction, parameters => NumericalGradient(objectiveFunction, parameters));
    //    var objectiveBaseOnly = ObjectiveFunction.Gradient(objectiveFunctionBaseOnly, parameters => NumericalGradient(objectiveFunctionBaseOnly, parameters));

    //    var solver = new BfgsMinimizer(1e-8, 1e-8, 10000);

    //    // Initial best guess from normal optimization
    //    var bestResult = solver.FindMinimum(objectiveBaseOnly, initialGuess);

    //    initialGuess = bestResult.MinimizingPoint;
    //    bestResult = solver.FindMinimum(objective, initialGuess);

    //    boreasBase = new cTransform(0, 0, 0, bestResult.MinimizingPoint[0].R2D(), bestResult.MinimizingPoint[1].R2D(), bestResult.MinimizingPoint[2].R2D());
    //    boreasTool = new cTransform(0, 0, 0, bestResult.MinimizingPoint[3].R2D(), bestResult.MinimizingPoint[4].R2D(), bestResult.MinimizingPoint[5].R2D());

    //    Console.WriteLine("\nTransformed Boreas w/ Transformation Parameters:");
    //    var result = testBoreasTransform(boreas, measured, boreasBase, boreasTool);

    //    // Output final best result
    //    Console.WriteLine("\nFinal Optimized Transformation Parameters:");
    //    for (int i = 0; i < bestResult.MinimizingPoint.Count; i++)
    //    {
    //        Console.WriteLine($"p{i + 1} = {bestResult.MinimizingPoint[i].R2D():F6}");
    //    }

    //    Console.WriteLine($"RMS Error = {result.rmsError:F6}");
    //    Console.WriteLine($"Average Error = {result.average:F6}");
    //    Console.WriteLine($"Standard Deviation = {result.stdev:F6}");
    //    Console.WriteLine($"A3S = {result.a3s:F6}");


    //    if (result.a3s > .1)
    //    {
    //        MessageBox.Show($"Boreas calibration results are poor.  A3s: {result.a3s:F3}");
    //    }

    //    else
    //    {
    //        MessageBox.Show($"Boreas calibration results. A3s: {result.a3s:F3}");
    //    }

    //    return result;
    //}

    public static FitAnalysis SolvePointCloudTransform(List<cXYZ> nominal, List<cXYZ> measured)
    {
        // Initialize
        cTransform xResult = new cTransform(0, 0, 0, 0, 0, 0);

        // Objective function: guess rX, rY, rZ
        Func<MathNet.Numerics.LinearAlgebra.Vector<double>, double> objectiveFunction = (MathNet.Numerics.LinearAlgebra.Vector<double> x) =>
        {
            double sumsq = 0;

            cTransform rotationOnly = new cTransform(0, 0, 0, x[0].R2D(), x[1].R2D(), x[2].R2D());

            if (nominal.Count != measured.Count)
                throw new InvalidOperationException("Mismatch between nominal and measured point counts.");

            double sumTx = 0, sumTy = 0, sumTz = 0;
            for (int i = 0; i < measured.Count; i++)
            {
                cPose measuredPose = new cPose(measured[i].x, measured[i].y, measured[i].z, 0, 0, 0);
                cXYZ rotatedMeasured = (rotationOnly.getLHT() * measuredPose.getLHT()).getXYZ();

                sumTx += nominal[i].x - rotatedMeasured.x;
                sumTy += nominal[i].y - rotatedMeasured.y;
                sumTz += nominal[i].z - rotatedMeasured.z;
            }

            double avgTx = sumTx / measured.Count;
            double avgTy = sumTy / measured.Count;
            double avgTz = sumTz / measured.Count;

            for (int i = 0; i < measured.Count; i++)
            {
                cPose measuredPose = new cPose(measured[i].x, measured[i].y, measured[i].z, 0, 0, 0);
                cXYZ rotatedMeasured = (rotationOnly.getLHT() * measuredPose.getLHT()).getXYZ();

                double movedX = rotatedMeasured.x + avgTx;
                double movedY = rotatedMeasured.y + avgTy;
                double movedZ = rotatedMeasured.z + avgTz;

                double dx = nominal[i].x - movedX;
                double dy = nominal[i].y - movedY;
                double dz = nominal[i].z - movedZ;

                sumsq += dx * dx + dy * dy + dz * dz;
            }

            return Math.Sqrt(sumsq);
        };

        // Numerical gradient calculation
        static MathNet.Numerics.LinearAlgebra.Vector<double> NumericalGradient(Func<MathNet.Numerics.LinearAlgebra.Vector<double>, double> objectiveFunction, MathNet.Numerics.LinearAlgebra.Vector<double> parameters, double epsilon = 1e-8)
        {
            int dim = parameters.Count;
            var gradient = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(dim);

            for (int j = 0; j < dim; j++)
            {
                var perturbedParams = parameters.Clone();
                perturbedParams[j] += epsilon;

                double f1 = objectiveFunction(perturbedParams);
                double f0 = objectiveFunction(parameters);

                gradient[j] = (f1 - f0) / epsilon;
            }

            return gradient;
        }

        // Initial guess (rotation only)
        var initialGuess = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfArray(new double[] { 0.0, 0.0, 0.0 });

        // Setup optimizer
        var objective = ObjectiveFunction.Gradient(objectiveFunction, parameters => NumericalGradient(objectiveFunction, parameters));
        var solver = new BfgsMinimizer(1e-8, 1e-8, 10000);

        // Solve
        var bestResult = solver.FindMinimum(objective, initialGuess);
        bestResult = solver.FindMinimum(objective, bestResult.MinimizingPoint);

        // Rebuild best rotation
        cTransform bestRotation = new cTransform(0, 0, 0,
            bestResult.MinimizingPoint[0].R2D(),
            bestResult.MinimizingPoint[1].R2D(),
            bestResult.MinimizingPoint[2].R2D());

        // Now recalculate the average translation properly
        double sumTxFinal = 0, sumTyFinal = 0, sumTzFinal = 0;
        for (int i = 0; i < measured.Count; i++)
        {
            cPose measuredPose = new cPose(measured[i].x, measured[i].y, measured[i].z, 0, 0, 0);
            cXYZ rotatedMeasured = (bestRotation.getLHT() * measuredPose.getLHT()).getXYZ();

            sumTxFinal += nominal[i].x - rotatedMeasured.x;
            sumTyFinal += nominal[i].y - rotatedMeasured.y;
            sumTzFinal += nominal[i].z - rotatedMeasured.z;
        }

        double avgTx = sumTxFinal / measured.Count;
        double avgTy = sumTyFinal / measured.Count;
        double avgTz = sumTzFinal / measured.Count;

        // Final full transform
        xResult = new cTransform(avgTx, avgTy, avgTz,
            bestResult.MinimizingPoint[0].R2D(),
            bestResult.MinimizingPoint[1].R2D(),
            bestResult.MinimizingPoint[2].R2D());

        // Output results
        Console.WriteLine("\nFinal Optimized Transformation Parameters:");
        Console.WriteLine($"tX = {avgTx:F6} mm");
        Console.WriteLine($"tY = {avgTy:F6} mm");
        Console.WriteLine($"tZ = {avgTz:F6} mm");
        Console.WriteLine($"rX = {bestResult.MinimizingPoint[0].R2D():F6} degrees");
        Console.WriteLine($"rY = {bestResult.MinimizingPoint[1].R2D():F6} degrees");
        Console.WriteLine($"rZ = {bestResult.MinimizingPoint[2].R2D():F6} degrees");

        var result = testTransform(nominal, measured, xResult);


        Console.WriteLine($"\nRMS Error = {result.rmsError:F6}");
        Console.WriteLine($"Average Error = {result.average:F6}");
        Console.WriteLine($"Standard Deviation = {result.stdev:F6}");
        Console.WriteLine($"A3S (avg + 3*stddev) = {result.a3s:F6}");


        return result;
    }

    public static FitAnalysis testTransform(List<cXYZ> nominal, List<cXYZ> measured, cTransform transform)
    {
        double sumsq = 0;
        double sum = 0;
        List<double> errors = new List<double>();


        if (nominal.Count != measured.Count)
            throw new InvalidOperationException("Mismatch between nominal and measured data counts.");

        for (int i = 0; i < nominal.Count; i++)
        {
            // Transform measured point into nominal space
            cPose measuredPose = new cPose(measured[i].x, measured[i].y, measured[i].z, 0, 0, 0);
            cXYZ transformedMeasured = (transform.getLHT() * measuredPose.getLHT()).getXYZ();

            // Calculate XYZ positional error to nominal
            double dx = nominal[i].x - transformedMeasured.x;
            double dy = nominal[i].y - transformedMeasured.y;
            double dz = nominal[i].z - transformedMeasured.z;

            double error = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            sumsq += error * error;
            sum += error;
            errors.Add(error);
        }

        int n = errors.Count;
        double rms = Math.Sqrt(sumsq);
        double avg = sum / n;
        double stddev = Math.Sqrt(errors.Sum(e => Math.Pow(e - avg, 2)) / n);
        FitAnalysis fit = new FitAnalysis();
        fit.residuals = errors;
        fit.average = avg;
        fit.stdev = stddev;
        fit.rmsError = rms;
        fit.xBaseTransform = transform;
        fit.a3s = avg + 3 * stddev;


        return fit;
    }

    public static BoreasFitAnalysis testBoreasTransform(
        List<cPose> boreas, List<cPose> measured,
        cTransform boreasBase, cTransform boreasTool)
    {
        double sumsq = 0;
        double sum = 0;
        List<double> errors = new List<double>();

        if (boreas.Count != measured.Count)
            throw new InvalidOperationException("Mismatch between boreas and measured data counts.");

        for (int i = 0; i < boreas.Count; i++)
        {
            // Transform boreasXYZ using xbase and xtool
            cTransform boreasXYZ = (boreasBase.getLHT() * boreas[i].getLHT() * boreasTool.getLHT()).getTransformEulerXYZ();

            Console.WriteLine($"{measured[i].rx:F3},{measured[i].ry:F3},{measured[i].rz:F3} - {boreasXYZ.rx:F3},{boreasXYZ.ry:F3},{boreasXYZ.rz:F3}");

            // Normalize angles
            double normRx = boreasXYZ.rx.m180p180();
            double normRy = boreasXYZ.ry.m180p180();
            double normRz = boreasXYZ.rz.m180p180();

            double deltaRx = (normRx - measured[i].rx).m180p180();
            double deltaRy = (normRy - measured[i].ry).m180p180();
            double deltaRz = (normRz - measured[i].rz).m180p180();

            double error = Math.Sqrt(deltaRx * deltaRx + deltaRy * deltaRy + deltaRz * deltaRz);

            sumsq += error * error;
            sum += error;
            errors.Add(error);
        }

        int n = errors.Count;
        double rms = Math.Sqrt(sumsq);
        double avg = sum / n;
        double stddev = Math.Sqrt(errors.Sum(e => Math.Pow(e - avg, 2)) / n);

        BoreasFitAnalysis fit = new BoreasFitAnalysis();
        fit.residuals = errors;
        fit.average = avg;
        fit.stdev = stddev;
        fit.rmsError = rms;
        fit.xBaseTransform = boreasBase;
        fit.xToolTransform = boreasTool;
        fit.a3s = avg + 3 * stddev;

        return fit;
    }

}

