using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using Pastel;
using System.Drawing;


namespace KukaKinematics;


#region Double Extension Methods
public static class doubleExtensionMethods
{
    /// <summary>
    /// Converts an angle from degrees to radians.
    /// </summary>
    /// <param name="Degrees">The angle in degrees.</param>
    /// <returns>The angle in radians.</returns>
    public static double DegreesToRadians(this double Degrees)
    {
        return (Degrees * Math.PI / 180.0);
    }

    /// <summary>
    /// Converts an angle from radians to degrees.
    /// </summary>
    /// <param name="Radians">The angle in radians.</param>
    /// <returns>The angle in degrees.</returns>
    public static double RadiansToDegrees(this double Radians)
    {
        return (Radians * 180.0 / Math.PI);
    }

    /// <summary>
    /// Converts an angle from degrees to radians.
    /// </summary>
    /// <param name="Degrees">The angle in degrees.</param>
    /// <returns>The angle in radians.</returns>
    public static double D2R(this double Degrees)
    {
        return (Degrees * Math.PI / 180.0);
    }

    /// <summary>
    /// Converts an angle from radians to degrees.
    /// </summary>
    /// <param name="Radians">The angle in radians.</param>
    /// <returns>The angle in degrees.</returns>
    public static double R2D(this double Radians)
    {
        return (Radians * 180.0 / Math.PI);
    }

    /// <summary>
    /// Converts an angle to a value between 0 and 360 degrees.
    /// </summary>
    /// <param name="input">The angle in degrees.</param>
    /// <returns>The angle in degrees, normalized to the range 0 to 360.</returns>
    public static double zeroto360(this double input)
    {
        double input_1 = m180p180(input);
        if (input_1 < 0.0)
            input_1 += 360.0;
        return input_1;
    }

    /// <summary>
    /// Converts an angle to a value between -180 and 180 degrees.
    /// </summary>
    /// <param name="input">The angle in degrees.</param>
    /// <returns>The angle in degrees, normalized to the range -180 to 180.</returns>
    public static double m180p180(this double input)
    {
        double d2r = Math.PI / 180.0;
        double sini = Math.Sin(d2r * input);
        double cosi = Math.Cos(d2r * input);
        return Math.Atan2(sini, cosi) / d2r;
    }
}

#endregion

#region Linear Homogeneous Transfomr Class (cLHT)
/// <summary>
/// /// an LHT transform (double[,] 4x4 class.  Natively this is Euler XYZ, but can be converted to other formats.)
/// </summary>
public class cLHT
{
    
    private double[,] m = new double[4, 4];

    public cLHT()
    {
        // Initialize to identity matrix
        m[0, 0] = 1;
        m[1, 1] = 1;
        m[2, 2] = 1;
        m[3, 3] = 1;
    }

    public cLHT(double[,] matrix)
    {
        m = matrix;
    }

    /// !*** transform data is now in degrees ***!
    public cLHT(cTransform input)
    {
        double x = input.x;
        double y = input.y;
        double z = input.z;
        double rx = input.rx.D2R();
        double ry = input.ry.D2R();
        double rz = input.rz.D2R();

        this.M = buildMatrixEulerXYZ(x, y, z, rx, ry, rz);
    }

    /// <summary>
    /// data in radians!
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="rx"></param>
    /// <param name="ry"></param>
    /// <param name="rz"></param>
    public cLHT(double x, double y, double z, double rx, double ry, double rz)
    {
        this.M = buildMatrixEulerXYZ(x, y, z, rx, ry, rz);
    }


    public double[,] GetUpperLeft3x3()
    {
        double[,] result = new double[3, 3];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                result[i, j] = m[i, j];
            }
        }
        return result;
    }

    /// <summary>
    /// some how the orientation is || to the flange coordinate system and NOT the FRS/World coordinate system. 
    /// I wonder how the TMac is configured to do this.  
    /// </summary>
    /// <param name="tmac"></param>
    public cLHT(cTMAC tmac, cTransform? xOrientation = null)
    {
        xOrientation ??= new cTransform();

        double qw = tmac.q0;
        double qx = tmac.q1;
        double qy = tmac.q2;
        double qz = tmac.q3;

        double magnitude = Math.Sqrt(qx * qx + qy * qy + qz * qz + qw * qw);
        if (magnitude < 1.0e-6)
        {
            qx = 0.0;
            qy = 0.0;
            qz = 0.0;
            qw = 1.0;
        }
        else
        {
            qx /= magnitude;
            qy /= magnitude;
            qz /= magnitude;
            qw /= magnitude;
        }

        double[] xyz = cHVD.convertHVDtoXYZ(tmac.h, tmac.v, tmac.d);
        double x = xyz[0];
        double y = xyz[1];
        double z = xyz[2];

        double qx2 = qx * qx;
        double qy2 = qy * qy;
        double qz2 = qz * qz;
        double qw2 = qw * qw;

        double xy = qx * qy;
        double xz = qx * qz;
        double xw = qx * qw;
        double yz = qy * qz;
        double yw = qy * qw;
        double zw = qz * qw;

        m[0, 0] = 1.0 - 2.0 * qy2 - 2.0 * qz2;
        m[0, 1] = 2.0 * xy - 2.0 * zw;
        m[0, 2] = 2.0 * xz + 2.0 * yw;
        m[0, 3] = 0;

        m[1, 0] = 2.0 * xy + 2.0 * zw;
        m[1, 1] = 1.0 - 2.0 * qx2 - 2.0 * qz2;
        m[1, 2] = 2.0 * yz - 2.0 * xw;
        m[1, 3] = 0;

        m[2, 0] = 2.0 * xz - 2.0 * yw;
        m[2, 1] = 2.0 * yz + 2.0 * xw;
        m[2, 2] = 1.0 - 2.0 * qx2 - 2.0 * qy2;
        m[2, 3] = 0;

        m[3, 0] = 0;
        m[3, 1] = 0;
        m[3, 2] = 0;
        m[3, 3] = 1;

        this.M = (this * xOrientation).M; //Orient the flangeFrame by the TMAC frame

        m[0, 3] = x;
        m[1, 3] = y;
        m[2, 3] = z;
    }

    static double[,] ensureOrthonormality(cLHT lht)
    {
        double[] input = lht.extractTranslationAndEulerXYZ();
        return lht.buildMatrixEulerXYZ(input[0], input[1], input[2], input[3], input[4], input[5]);
    }
    
    public cXYZ getXYZ()
    {

       return new cXYZ(m[0, 3], m[1, 3], m[2, 3]);
    }
    
    //twr start from here, we want to return degrees so it is convenient for your comrades.
    /// <summary>
    /// returns orientations in degrees.
    /// </summary>
    /// <returns></returns>
    public cPose getPoseEulerXYZ()
    {
        double[] result = extractTranslationAndEulerXYZ();
        cPose pose = new cPose();
        pose.x = result[0];
        pose.y = result[1];
        pose.z = result[2];
        pose.rx = result[3].R2D();
        pose.ry = result[4].R2D();
        pose.rz = result[5].R2D();
        return pose;
    }

    /// <summary>
    /// returns orientations in degrees
    /// </summary>
    /// <returns></returns>
    public cPose getPoseFixedZYX()
    {
        double[] result = extractTranslationAndFixedZYX();
        cPose pose = new cPose();
        pose.x = result[0];
        pose.y = result[1];
        pose.z = result[2];
        pose.rx = result[3].R2D();
        pose.ry = result[4].R2D();
        pose.rz = result[5].R2D();
        return pose;
    }

    public double[,] M
    {
        get 
        { 
            return m;
        }
        set 
        { 
            m = value;
        }
    }

    /// <summary>
    /// Transforms are in degrees
    /// </summary>
    /// <returns></returns>
    public cTransform getTransformEulerZYX()
    {
        cPose d = getPoseEulerZYX();
        cTransform result = new cTransform();
        result.x = d.x;
        result.y = d.y;
        result.z = d.z;
        result.rx = d.rx;
        result.ry = d.ry;
        result.rz = d.rz;
        return result;
    }

    /// <summary>
    /// Transforms are in degrees
    /// </summary>
    /// <returns></returns>
    public cTransform getTransformEulerXYZ()
    {
        cPose pose = getPoseEulerXYZ();
        cTransform result = new cTransform();
        result.x = pose.x;
        result.y = pose.y;
        result.z = pose.z;
        result.rx = pose.rx;
        result.ry = pose.ry;
        result.rz = pose.rz;
        return result;
    }

    /// <summary>
    /// Transforms are in degrees
    /// </summary>
    /// <returns></returns>
    public cTransform getTransformFixedZYX()
    {
        cPose d = getPoseFixedZYX();
        cTransform result = new cTransform();
        result.x = d.x;
        result.y = d.y;
        result.z = d.z;
        result.rx = d.rx;
        result.ry = d.ry;
        result.rz = d.rz;
        return result;
    }

    //Verified
    /// <summary>
    /// Pose is in degrees
    /// </summary>
    /// <returns></returns>
    public cPose getPoseEulerZYX()
    {
        cPose result = new cPose();
        double[] i = { m[0, 0], m[1, 0], m[2, 0] };
        double[] j = { m[0, 1], m[1, 1], m[2, 1] };
        double[] k = { m[0, 2], m[1, 2], m[2, 2] };
        double[] r = { m[0, 3], m[1, 3], m[2, 3] };

        double rx = Math.Atan2(i[1], i[0]);
        double ry = Math.Atan2(-i[2], Math.Sqrt(i[0] * i[0] + i[1] * i[1]));
        double rz = Math.Atan2(j[2], k[2]);

        result.x = r[0];
        result.y = r[1];
        result.z = r[2];
        result.rx = rx.R2D();
        result.ry = ry.R2D();
        result.rz = rz.R2D();
        
        return result;
    }

    private double[] extractTranslationAndFixedZYX()
    {
        return extractTranslationAndFixedZYX(this.M);
    }

    private double[] extractTranslationAndFixedZYX(double[,] m)
    {
        double[] result = new double[6];

        // Extract translation components
        double[] i = { m[0, 0], m[1, 0], m[2, 0] };
        double[] j = { m[0, 1], m[1, 1], m[2, 1] };
        double[] k = { m[0, 2], m[1, 2], m[2, 2] };
        double[] r = { m[0, 3], m[1, 3], m[2, 3] };

        //First Solution
        double rY = Math.Atan2(k[0], Math.Sqrt(k[1] * k[1] + k[2] * k[2]));
        double cosry = Math.Cos(rY);

        double rZ;
        double rX;
        if (Math.Abs(cosry) <= 1.0e-6)
        {
            if (k[0] > 0.0)
            {
                rZ = 0.0;
                rX = Math.Atan2(j[0], i[0]);
            }
            else
            {
                rZ = 0.0;
                rX = -Math.Atan2(j[0], i[0]);
            }
        }
        else
        {
            rZ = Math.Atan2(-j[0], i[0]);
            rX = Math.Atan2(-k[1], k[2]);
        }


        result[0] = r[0];
        result[1] = r[1];
        result[2] = r[2];
        result[3] = rX;
        result[4] = rY;
        result[5] = rZ;

        return result;
    }

    /// <summary>
    /// when we use pose, degrees are the units.
    /// </summary>
    /// <param name="eulerXYZ"></param>
    public void setTransformFromEulerXYZ(cPose eulerXYZ)
    {
        double x = eulerXYZ.x;
        double y = eulerXYZ.y;
        double z = eulerXYZ.z;
        double rx = eulerXYZ.rx.D2R();
        double ry = eulerXYZ.ry.D2R();
        double rz = eulerXYZ.rz.D2R();

        this.M = buildMatrixEulerXYZ(x, y, z, rx, ry, rz);
    }

    /// <summary>
    /// when we use pose, degrees are the units
    /// </summary>
    /// <param name="eulerZYX"></param>
    public void setTransformFromEulerZYX(cPose eulerZYX)
    {
        double x = eulerZYX.x;
        double y = eulerZYX.y;
        double z = eulerZYX.z;
        double rx = eulerZYX.rx.D2R();
        double ry = eulerZYX.ry.D2R();
        double rz = eulerZYX.rz.D2R();
        cLHT rzm = new cLHT( 0,0,0,0,0,rz);
        cLHT rym = new cLHT( 0,0,0,0,ry,0);
        cLHT rxm = new cLHT( 0,0,0,rx,0,0);
        cLHT r = rzm * rym * rxm;
        this.M = r.M;
        this.M[0, 3] = x;
        this.M[1, 3] = y;
        this.M[2, 3] = z;
        this.M[3, 3] = 1;
        this.M[3, 0] = 0;
        this.M[3, 1] = 0;
        this.M[3, 2] = 0;           
    }

    /// <summary>
    /// when we use cPose, angles are in degrees!
    /// </summary>
    /// <param name="fixedZYX"></param>
    public void setTransformFromFixedAngleZYX(cPose fixedZYX)
    {
        double x = fixedZYX.x;
        double y = fixedZYX.y;
        double z = fixedZYX.z;
        double rx = fixedZYX.rx.D2R();
        double ry = fixedZYX.ry.D2R();
        double rz = fixedZYX.rz.D2R();

        this.M = buildMatrixFixedZYX(x, y, z, rz, ry, rx);
    }

    private double[,] buildMatrixFixedZYX(double x, double y, double z, double gamma, double beta, double alpha)
    {
        double[,] result = new double[4, 4];
        double cosgamma = Math.Cos(gamma);
        double singamma = Math.Sin(gamma);
        double cosbeta = Math.Cos(beta);
        double sinbeta = Math.Sin(beta);
        double cosalpha = Math.Cos(alpha);
        double sinalpha = Math.Sin(alpha);

        // Compute rotation matrix elements from fixed angles
        double r11 = cosbeta * cosgamma;
        double r12 = -cosbeta * singamma;
        double r13 = sinbeta;

        double r21 = sinalpha * sinbeta * cosgamma + cosalpha * singamma;
        double r22 = -sinalpha * sinbeta * singamma + cosalpha * cosgamma;
        double r23 = -sinalpha * cosbeta;

        double r31 = -cosalpha * sinbeta * cosgamma + sinalpha * singamma;
        double r32 = cosalpha * sinbeta * singamma + sinalpha * cosgamma;
        double r33 = cosalpha * cosbeta;

        // Construct the 4x4 transformation matrix
        result[0, 0] = r11; result[0, 1] = r12; result[0, 2] = r13; result[0, 3] = x;
        result[1, 0] = r21; result[1, 1] = r22; result[1, 2] = r23; result[1, 3] = y;
        result[2, 0] = r31; result[2, 1] = r32; result[2, 2] = r33; result[2, 3] = z;
        result[3, 0] = 0; result[3, 1] = 0; result[3, 2] = 0; result[3, 3] = 1;

        return result;
    }


    /// <summary>
    /// returns rotations in radians
    /// </summary>
    /// <returns></returns>
    private double[] extractTranslationAndEulerXYZ()
    {
        return extractTranslationAndEulerXYZ(this.M);
    }

    /// <summary>
    /// returns rotations in radians
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    public double[] extractTranslationAndEulerXYZ(double[,] m)
    {
        double[] result = new double[6];

        // Extract translation components
        double[] i = { m[0, 0], m[1, 0], m[2, 0] };
        double[] j = { m[0, 1], m[1, 1], m[2, 1] };
        double[] k = { m[0, 2], m[1, 2], m[2, 2] };
        double[] r = { m[0, 3], m[1, 3], m[2, 3] };


        //First Solution
        double rX = Math.Atan2(-k[1], k[2]);
        double rY = Math.Atan2(k[0], Math.Sqrt(k[1] * k[1] + k[2] * k[2]));
        double rZ = Math.Atan2(-j[0], i[0]);

        result[0] = r[0];
        result[1] = r[1];
        result[2] = r[2];
        result[3] = rX;
        result[4] = rY;
        result[5] = rZ;

        return result;
    }
    /// <summary>
    /// radians for rx, ry, rz
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="rx"></param>
    /// <param name="ry"></param>
    /// <param name="rz"></param>
    /// <returns></returns>
    private double[,] buildMatrixEulerXYZ(double x, double y, double z, double rx, double ry, double rz)
    {
        double[,] matrix = new double[4, 4];

        // Precompute cosines and sines of the Euler angles
        double cosX = Math.Cos(rx);
        double sinX = Math.Sin(rx);
        double cosY = Math.Cos(ry);
        double sinY = Math.Sin(ry);
        double cosZ = Math.Cos(rz);
        double sinZ = Math.Sin(rz);

        // Compute rotation matrix elements
        double r11 = cosY * cosZ;
        double r12 = -cosY * sinZ;
        double r13 = sinY;

        double r21 = sinX * sinY * cosZ + cosX * sinZ;
        double r22 = -sinX * sinY * sinZ + cosX * cosZ;
        double r23 = -sinX * cosY;

        double r31 = -cosX * sinY * cosZ + sinX * sinZ;
        double r32 = cosX * sinY * sinZ + sinX * cosZ;
        double r33 = cosX * cosY;

        // Construct the 4x4 transformation matrix
        matrix[0, 0] = r11; matrix[0, 1] = r12; matrix[0, 2] = r13; matrix[0, 3] = x;
        matrix[1, 0] = r21; matrix[1, 1] = r22; matrix[1, 2] = r23; matrix[1, 3] = y;
        matrix[2, 0] = r31; matrix[2, 1] = r32; matrix[2, 2] = r33; matrix[2, 3] = z;
        matrix[3, 0] = 0; matrix[3, 1] = 0; matrix[3, 2] = 0; matrix[3, 3] = 1;

        return matrix;
    }



    // Helper method to multiply two matrices
    private static double[,] multiplyLHT(double[,] a, double[,] b)
    {
        double[,] result = new double[4, 4];
    
        for (int i = 0; i < 3; i++) // Only iterate over the first three rows
        {
            for (int j = 0; j < 4; j++) // Iterate over all columns
            {
                result[i, j] = a[i, 0] * b[0, j] + a[i, 1] * b[1, j] + a[i, 2] * b[2, j];
                // For the last column, add the element from the fourth column of 'a', which is always 1 for the last element
                if (j == 3) result[i, j] += a[i, 3];
            }
        }
    
        // Set the fourth row to [0, 0, 0, 1] directly, as it's a constant for linear homogeneous transformations
        result[3, 0] = 0;
        result[3, 1] = 0;
        result[3, 2] = 0;
        result[3, 3] = 1;
    
        return result;
    }

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Matrix:");
        for (int i = 0; i < 4; i++) // Assuming M is a 4x4 matrix
        {
            for (int j = 0; j < 4; j++)
            {
                // Assuming M[i, j] accesses the matrix element at row i, column j
                // Format for alignment and readability
                sb.AppendFormat("{0,10:F4} ", M[i, j]);
            }
            sb.AppendLine(); // New line for each row
        }
        return sb.ToString();
    }

    public static cLHT operator *(cLHT a, cLHT b)
    {
        // Build rotation matrices for both transforms
        double[,] aMatrix = a.M;
        double[,] bMatrix = b.M;

        // Multiply the matrices
        double[,] resultMatrix = multiplyLHT(aMatrix, bMatrix);

        cLHT resultTransform = new cLHT();
        resultTransform.M = resultMatrix;

        return resultTransform;
    }

    public static cXYZ operator *(cLHT transform, cXYZ point)
    {
        double x = transform.M[0, 0] * point.x + transform.M[0, 1] * point.y + transform.M[0, 2] * point.z + transform.M[0, 3];
        double y = transform.M[1, 0] * point.x + transform.M[1, 1] * point.y + transform.M[1, 2] * point.z + transform.M[1, 3];
        double z = transform.M[2, 0] * point.x + transform.M[2, 1] * point.y + transform.M[2, 2] * point.z + transform.M[2, 3];
       
        return new cXYZ(x, y, z);
    }

    public static cLHT operator !(cLHT a)
    {
        // Placeholder for the actual matrix inversion logic
        double[,] invertedMatrix = InvertMatrix(a.M);

        cLHT result = new cLHT();
        result.M = invertedMatrix;
        return result;
    }

        // Override the == operator
    public static bool operator ==(cLHT a, cLHT b)
    {
        if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
        {
            return ReferenceEquals(a, b);
        }

        // Compare each element in the matrices
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (Math.Abs(a.M[i, j] - b.M[i, j]) > 1.0e-6)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Override the != operator (often done in conjunction with ==)
    public static bool operator !=(cLHT a, cLHT b)
    {
        return !(a == b);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not cLHT other) return false;
        return this == other;
    }

    public override int GetHashCode()
    {
        int hash = 17;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                hash = hash * 23 + M[i, j].GetHashCode();
            }
        }
        return hash;
    }

    private static double[,] InvertMatrix(double[,] matrix)
    {
        // Assuming matrix is a 4x4 linear homogeneous transformation matrix
        double[,] inverse = new double[4, 4];

        // Transpose the rotation part (top-left 3x3)
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                inverse[i, j] = matrix[j, i];
            }
        }

        // Invert the translation part
        for (int i = 0; i < 3; i++)
        {
            inverse[i, 3] = 0;
            for (int j = 0; j < 3; j++)
            {
                inverse[i, 3] -= inverse[i, j] * matrix[j, 3];
            }
        }

        // Last row remains the same for homogeneous transformation matrices
        inverse[3, 0] = 0;
        inverse[3, 1] = 0;
        inverse[3, 2] = 0;
        inverse[3, 3] = 1;

        return inverse;
    }

}

#endregion

#region Vector Class for simple linear algebra operations

public class cVector3d
{
    public cVectorDouble v;
    public cVector3d()
    {
        v = new cVectorDouble(3);
        v[0] = 0.0;
        v[1] = 0.0;
        v[2] = 0.0;
    }
    public cVector3d(double x, double y, double z)
    {
        v = new cVectorDouble(3);
        v[0] = x;
        v[1] = y;
        v[2] = z;
    }
}
public class cVectorDouble
{
    double[] doubles;

    public cVectorDouble(int capacity)
    { 
        doubles = new double[capacity]; 
    }

    // Overload '-' operator
    public static cVectorDouble operator -(cVectorDouble a, cVectorDouble b)
    {
        if (a.doubles.Length != b.doubles.Length)
        {
            throw new InvalidOperationException("Cannot perform operation on vectors of different lengths.");
        }

        cVectorDouble result = new cVectorDouble(a.doubles.Length);

        for (int i = 0; i < a.doubles.Length; i++)
        {
            result.doubles[i] = a.doubles[i] - b.doubles[i];
        }

        return result;
    }

        // Overload '+' operator
    public static cVectorDouble operator +(cVectorDouble a, cVectorDouble b)
    {
        if (a.doubles.Length != b.doubles.Length)
        {
            throw new InvalidOperationException("Cannot perform operation on vectors of different lengths.");
        }

        cVectorDouble result = new cVectorDouble(a.doubles.Length);

        for (int i = 0; i < a.doubles.Length; i++)
        {
            result.doubles[i] = a.doubles[i] + b.doubles[i];
        }

        return result;
    }

        // Overload '*' operator for scalar multiplication
    public static cVectorDouble operator *(cVectorDouble a, double scalar)
    {
        cVectorDouble result = new cVectorDouble(a.doubles.Length);

        for (int i = 0; i < a.doubles.Length; i++)
        {
            result.doubles[i] = a.doubles[i] * scalar;
        }

        return result;
    }

    public static cVectorDouble operator *(double scalar,  cVectorDouble a)
    {
        cVectorDouble result = new cVectorDouble(a.doubles.Length);

        for (int i = 0; i < a.doubles.Length; i++)
        {
            result.doubles[i] = a.doubles[i] * scalar;
        }

        return result;
    }

    // Indexer to allow getting and setting values like an array
    public double this[int index]
    {
        get { return doubles[index]; }
        set { doubles[index] = value; }
    }

    // Overload '*' operator for dot product
    public static double operator *(cVectorDouble a, cVectorDouble b)
    {
        if (a.doubles.Length != b.doubles.Length)
        {
            throw new InvalidOperationException("Cannot perform operation on vectors of different lengths.");
        }

        double result = 0;

        for (int i = 0; i < a.doubles.Length; i++)
        {
            result += a.doubles[i] * b.doubles[i];
        }

        return result;
    }

    public static cVectorDouble Cross(cVectorDouble a, cVectorDouble b)
    {
        if (a.doubles.Length != 3 || b.doubles.Length != 3)
        {
            throw new InvalidOperationException("Cross product is only defined for 3D vectors.");
        }

        cVectorDouble result = new cVectorDouble(3);
        result[0] = a.doubles[1] * b.doubles[2] - a.doubles[2] * b.doubles[1];
        result[1] = a.doubles[2] * b.doubles[0] - a.doubles[0] * b.doubles[2];
        result[2] = a.doubles[0] * b.doubles[1] - a.doubles[1] * b.doubles[0];
        return result;
    }

    public cVectorDouble Cross(cVectorDouble b)
    {
        if (doubles.Length != 3 || b.doubles.Length != 3)
        {
            throw new InvalidOperationException("Cross product is only defined for 3D vectors.");
        }

        cVectorDouble result = new cVectorDouble(3);
        result[0] = doubles[1] * b.doubles[2] - doubles[2] * b.doubles[1];
        result[1] = doubles[2] * b.doubles[0] - doubles[0] * b.doubles[2];
        result[2] = doubles[0] * b.doubles[1] - doubles[1] * b.doubles[0];
        return result;
    }


    public static cVectorDouble operator /(cVectorDouble a, double b)
    {
        cVectorDouble result = new cVectorDouble(a.doubles.Length);

        for (int i = 0; i < a.doubles.Length; i++)
        {
            result.doubles[i] = a.doubles[i] / b;
        }

        return result;
    }

    // Get magnitude of the vector
    public double GetMagnitude()
    {
        double sumOfSquares = 0;

        for (int i = 0; i < doubles.Length; i++)
        {
            sumOfSquares += Math.Pow(doubles[i], 2);
        }

        return Math.Sqrt(sumOfSquares);
    }

    // Normalize the vector
    public void NormalizeMe()
    {
        double magnitude = GetMagnitude();

        for (int i = 0; i < doubles.Length; i++)
        {
            doubles[i] /= magnitude;
        }
    }

    // Get a normalized version of the vector
    public cVectorDouble GetNormalVector()
    {
        double magnitude = GetMagnitude();
        cVectorDouble result = new cVectorDouble(doubles.Length);

        for (int i = 0; i < doubles.Length; i++)
        {
            result[i] = doubles[i] / magnitude;
        }

        return result;
    }
}
#endregion

#region XYZ Class

public struct sXYZ
{
    public double x;
    public double y;
    public double z;
}

public class cXYZ
{
    public double x, y, z;


    public double X
    {
        get { return x; }
        set { this.x = value; }
    }

    public double Y
    {
        get { return y; }
        set { this.y = value; }
    }

    public double Z
    {
        get { return z; }
        set { this.z = value; }
    }


    public sXYZ getStruct()
    {
        sXYZ result = new sXYZ();
        result.x = x;
        result.y = y;
        result.z = z;
        return result;
    }
    public double getMagnitude()
    {
        return Math.Sqrt(x * x + y * y + z * z);
    }


    public cXYZ(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public cXYZ()
    {
        x = 0;
        y = 0;
        z = 0;
    }
    /// <summary>
    /// Addition of two Xyz
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static cXYZ operator +(cXYZ a, cXYZ b)
    {
        return new cXYZ(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    /// <summary>
    /// Subtraction of two Xyz
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static cXYZ operator -(cXYZ a, cXYZ b)
    {
        return new cXYZ(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    /// <summary>
    /// Multiplication of two Xyz, returning scalar
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static double operator *(cXYZ a, cXYZ b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    /// <summary>
    /// Multiplication of an Xyz by a scalar, returning an Xyz
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static cXYZ operator *(cXYZ a, double b)
    {
        return new cXYZ(a.x * b, a.y * b, a.z * b);
    }

    /// <summary>
    /// Division of an Xyz by a scalar, returning an Xyz
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static cXYZ operator /(cXYZ a, double b)
    {
        return new cXYZ(a.x / b, a.y / b, a.z / b);
    }

    /// <summary>
    /// Cross product of two Xyz, returning an Xyz
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static cXYZ operator ^(cXYZ a, cXYZ b)
    {
        return new cXYZ(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x);
    }

    /// <summary>
    /// Gives the average between two Xyz, returning an Xyz
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static cXYZ operator &(cXYZ a, cXYZ b)
    {
        return new cXYZ((a.x + b.x) / 2.0, (a.y + b.y) / 2.0, (a.z + b.z) / 2.0);
    }

    /// <summary>
    /// Returns magnitude/two-norm of an cXYZ
    /// </summary>
    /// <returns></returns>
    public double Magnitude()
    {
        return Math.Sqrt(x * x + y * y + z * z);
    }

    /// <summary>
    /// Returns the normalized vector of an cXYZ
    /// </summary>
    /// <returns></returns>
    public cXYZ Normalize()
    {
        double magnitude = Magnitude();
        return new cXYZ(x / magnitude, y / magnitude, z / magnitude);
    }


    // Function to compute the centroid of a set of 3 points, may extend to many points.
    public static cXYZ ComputeCentroid(List<cXYZ> points)
    {
        if (points.Count < 3)
            throw new ArgumentException("At least 3 points are required.");

        cXYZ sum = new cXYZ(0, 0, 0);
        foreach (var point in points)
        {
            sum += point;
        }
        return sum / points.Count;
    }

    // Function to center the points
    public static List<cXYZ> CenterPoints(List<cXYZ> points)
    {
        cXYZ centroid = ComputeCentroid(points);

        // Center each point by subtracting the centroid
        List<cXYZ> centeredPoints = new List<cXYZ>();

        for (int i = 0; i < points.Count; i++)
        {
            centeredPoints.Add(points[i] - centroid);
        }

        return centeredPoints;
    }

    public double[] ToArray()
    {
        double[] result = { x, y, z };
        return result;
    }

    // Function to compute the cross-covariance matrix
    public static cLHT ComputeCrossCovariance(List<cXYZ> nominal, List<cXYZ> measured)
    {
        int numPoints = nominal.Count;
        double[,] covarianceMatrix = new double[3, 3];

        // Accumulate the covariance matrix
        for (int i = 0; i < numPoints; i++)
        {
            double[] n = nominal[i].ToArray();
            double[] m = measured[i].ToArray();

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    covarianceMatrix[row, col] += n[row] * m[col];
                }
            }
        }

        return new cLHT(covarianceMatrix);
    }


}
#endregion

#region HVD Class, just a way to store some methods
public class cHVD
{
    public double h;
    public double v;
    public double d;

    public cHVD()
    {
        h = v = d = 0;
    }
    public cHVD(double h, double v, double d)
    {
        this.h = h;
        this.v = v;
        this.d = d;
    }


/// <summary>
/// returns HVD in degrees and meters
/// </summary>
/// <param name="point"> a cartisian point </param>
/// <param name="xform"> the transform (usually instrumentToWorld</param>
/// <returns></returns>
    public static cHVD HVDfromTransformedXYZ(cXYZ point, cLHT xform)
    {
        cXYZ xPoint = !xform * point;
        cXYZ hvd = convertCartisianToHVD(xPoint);
        hvd.x = hvd.x.R2D().m180p180();
        hvd.y = hvd.y.R2D().m180p180();

        cHVD result = new cHVD(hvd.x, hvd.y, hvd.z);
        return result;
    }

    /// <summary>
    /// expecting data in radians and meters
    /// </summary>
    /// <param name="hin"></param>
    /// <param name="vin"></param>
    /// <param name="din"></param>
    /// <returns></returns>
    public static double[] convertHVDtoXYZ(double hin, double vin, double din)
    {
        double h = 90.0.D2R() - hin;	// switch from radians to deg and shift for horiz zero to x-axis
        double v = vin;
        double d = din * 1000.0;    // switch from meters to mm

        double x = d * Math.Sin(v) * Math.Cos(h);
        double y = d * Math.Sin(v) * Math.Sin(h);
        double z = d * Math.Cos(v);

        return new double[] { x, y, z };
    }

    public static cXYZ convertHVDtoXYZReturncXYZ(double hin, double vin, double din)
    {
        double[] result = convertHVDtoXYZ(hin, vin, din);
        return new cXYZ(result[0], result[1], result[2]);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="coord"></param>
    /// <returns></returns>
    public static cXYZ convertCartisianToHVD(cXYZ coord)
    {
        double smallValue = 1.0e-6;
        cXYZ hvd = new cXYZ();

        hvd.z = coord.getMagnitude();
        if (hvd.z < smallValue)
        {
            return hvd;
        }

        if (Math.Abs(coord.x) < smallValue && Math.Abs(coord.y) < smallValue)
        {
            if (coord.z >= 0.0)
            {
                hvd.y = 0.0;
            }
            else
            {
                hvd.y = 180.0;
            }
            hvd.x = 0.0;
            return hvd;
        }
        hvd.x = Math.Atan2(coord.y, coord.x);
        hvd.y = Math.Acos(coord.z / hvd.z);


        hvd.x = (90.0.D2R() - hvd.x);
        hvd.z = hvd.z / 1000.0;
        return hvd;
    }
}



#endregion

#region cTMAC Class

public class cTMAC
{
    public double h;
    public double v;
    public double d;
    public double q0; //qw
    public double q1; //qx
    public double q2; //qy
    public double q3; //qz

    public int baseTransformId = 0;
    public int toolTransformId = 0;

    public cTMAC()
    {
        h = v = d = q0 = q1 = q2 = q3 = 0.0;
    }
    public cTMAC(double h, double v, double d, double q0, double q1, double q2, double q3)
    {
        this.h = h;
        this.v = v;
        this.d = d;
        this.q0 = q0;
        this.q1 = q1;
        this.q2 = q2;
        this.q3 = q3;
    }

    public cLHT getLHT()
    {
        return new cLHT(this);
    }

    public void setTransformIds(int baseTransformId,  int toolTransformId)
    {
        this.baseTransformId = baseTransformId;
        this.toolTransformId = toolTransformId;
    }

}

#endregion

#region cPose Class
public class cPose
{
    public double x;
    public double y;
    public double z;
    public double rx;
    public double ry;
    public double rz;

    public cPose()
    {
        this.x = 0.0;
        this.y = 0.0;
        this.z = 0.0;
        this.rx = 0.0;
        this.ry = 0.0;
        this.rz = 0.0;
    }
    /// cPose and cTransform are in degrees
    public cPose(cTransform input)
    {
        this.x = input.x;
        this.y = input.y;
        this.z = input.z;
        this.rx = input.rx;
        this.ry = input.ry;
        this.rz = input.rz;
    }
    // cPose is in degrees
    public cPose(double x, double y, double z, double rx, double ry, double rz)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.rx = rx;
        this.ry = ry;
        this.rz = rz;
    }

    public static cPose operator -(cPose a, cPose b)
    {
        cPose result = new cPose();
        result.x = a.x - b.x;
        result.y = a.y - b.y;
        result.z = a.z - b.z;
        result.rx = (a.rx - b.rx).m180p180();
        result.ry = (a.ry - b.ry).m180p180();
        result.rz = (a.rz - b.rz).m180p180();
        return result;
    }

    /// trasnform is in degrees
    public cTransform getTransform()
    {
        cTransform result = new cTransform();
        result.x = this.x;
        result.y = this.y;
        result.z = this.z;
        result.rx = this.rx;
        result.ry = this.ry;
        result.rz = this.rz;
        return result;
    }

    public cLHT getLHT()
    {
        cTransform result = new cTransform();
        result.x = this.x;
        result.y = this.y;
        result.z = this.z;
        result.rx = this.rx;
        result.ry = this.ry;
        result.rz = this.rz;
        return result.getLHT();
    }

    public cXYZ getXYZ()
    {
        cXYZ result = new cXYZ(this.x, this.y, this.z);
        return result;
    }

    // implement interface for X, Y, Z, rX, rY, rZ
    public double X
    {
        get { return x; }
        set { this.x = value; }
    }

    public double Y
    {
        get { return y; }
        set { this.y = value; }
    }

    public double Z
    {
        get { return z; }
        set { this.z = value; }
    }

    public double rX
    {
        get { return rx; }
        set { this.rx = value; }
    }

    public double rY
    {   
        get { return ry; }
        set { this.ry = value; }
    }

    public double rZ
    {
        get { return rz; }
        set { this.rz = value; }
    }

    /// <summary>
    /// Calculates the Euclidean distance between two calibration points.
    /// </summary>
    /// <returns>The Euclidean distance between the two calibration points.</returns>
    public static double CalculateDistance(cPose a, cPose b)
    {
        return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) + Math.Pow(a.Z - b.Z, 2));
    }

    /// <summary>
    /// Calculates the orientation distance between two calibration points.
    /// </summary>
    /// <returns>The orientation distance between the two calibration points.</returns>
    public static double CalculateOrientationDistance(cPose a, cPose b)
    {
        double dx = Math.Abs(a.rX - b.rX).m180p180();
        double dy = Math.Abs(a.rY - b.rY).m180p180();
        double dz = Math.Abs(a.rZ - b.rZ).m180p180();

        return Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2) + Math.Pow(dz, 2));
    }
}

// a bare pose class for transfer to the robots S_Transform
public class RoboPose
{
    public float x;
    public float y;
    public float z;
    public float rx;
    public float ry;
    public float rz;

    public RoboPose()
    {
        this.x = 0.0f;
        this.y = 0.0f;
        this.z = 0.0f;
        this.rx = 0.0f;
        this.ry = 0.0f;
        this.rz = 0.0f;
    }

    public RoboPose(float x, float y, float z, float rx, float ry, float rz)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.rx = rx;
        this.ry = ry;
        this.rz = rz;
    }

    public RoboPose(cPose pose)
    {
        this.x = (float)pose.x;
        this.y = (float)pose.y;
        this.z = (float)pose.z;
        this.rx = (float)pose.rx;
        this.ry = (float)pose.ry;
        this.rz = (float)pose.rz;
    }
}


#endregion
