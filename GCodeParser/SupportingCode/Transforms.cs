using System;


namespace KukaKinematics;


public class cTransform
{
    public double x;
    public double y;
    public double z;
    public double rx;
    public double ry;
    public double rz;

    public int id = -1;

    public cTransform()
    {
        x = 0; y = 0; z = 0; rx = 0; ry = 0; rz = 0;
    }

    /// <summary>
    /// paste contstructor.
    /// Paste in the transform from copy matrix in SA
    /// </summary>
    /// <param name="PastedStringFromSA"></param>
    public cTransform(int id, string PastedStringFromSA)
    {
        this.id = id;
        string[] numbers = PastedStringFromSA.Split(' ');

        double[,] matrix = new double[4, 4];

        int index = 0;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                matrix[i, j] = double.Parse(numbers[index]);
                index++;
            }
        }
        this.Matrix = matrix;            
    }

    public cTransform(double[,] M)
    {
        Matrix = M;
    }

    public cTransform(int id, double x, double y, double z, double rx, double ry, double rz)
    {
        this.id = id;
        this.x = x;
        this.y = y;
        this.z = z;
        this.rx = rx;
        this.ry = ry;
        this.rz = rz;
    }

    public cTransform(double x, double y, double z, double rx, double ry, double rz)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.rx = rx;
        this.ry = ry;
        this.rz = rz;
    }
    public double[,] Matrix
    {
        get
        {
            cLHT test = new cLHT(this);
            return test.M;
        }
        set
        {
            cLHT test = new cLHT(value);
            cPose args2 = test.getPoseEulerXYZ();

            x = args2.x;
            y = args2.y;
            z = args2.z;
            rx = args2.rx;
            ry = args2.ry;
            rz = args2.rz;
        }
    }

    public cLHT getLHT()
    {
        cLHT result = new cLHT(this);
        return result;
    }

    public cPose getPose()
    {
        cPose pose = new cPose();
        pose.x = this.x;
        pose.y = this.y;
        pose.z = this.z;
        pose.rx = this.rx;
        pose.ry = this.ry;
        pose.rz = this.rz;
        return pose;
    }

    //implent returning cXYZ
    public cXYZ getXYZ()
    {
        cXYZ result = new cXYZ(this.x, this.y, this.z);
        return result;
    }

    public static cXYZ operator *(cTransform transform, cXYZ point)
    {
        cLHT xform = new cLHT(transform);
        cXYZ result = xform * point;
        return result;
    }

    public static cXYZ operator *(cXYZ point, cTransform transform)
    {
        cLHT xform = new cLHT(transform);
        cLHT pointLHT = new cLHT(point.x, point.y, point.z, 0, 0, 0);
        cLHT lhtResult = pointLHT * xform;
        cXYZ result = new cXYZ(lhtResult.M[0,3], lhtResult.M[1,3], lhtResult.M[2,3]);
        return result;
    }

    //override the ! operator:
    public static cTransform operator !(cTransform transform)
    {
        cLHT inputXform = new cLHT(transform);
        inputXform = !inputXform;

        cPose xformargs = inputXform.getPoseEulerXYZ();

        cTransform result = new cTransform( xformargs.x, 
                                            xformargs.y, 
                                            xformargs.z, 
                                            xformargs.rx, 
                                            xformargs.ry, 
                                            xformargs.rz);
        return result;
    }


    public static cTransform operator *(cTransform transform1, cTransform transform2)
    {
        cLHT xform1 = new cLHT(transform1);
        cLHT xform2 = new cLHT(transform2);
        cLHT lht = xform1 * xform2;
        cTransform result = new cTransform(lht.M);
        return result;
    }

    public static cLHT operator *(cLHT transform1, cTransform transform2)
    {
        cLHT result = transform1 * new cLHT(transform2);
        return result;
    }

    public static cLHT operator *(cTransform transform1, cLHT transform2)
    {
        cLHT result = new cLHT(transform1) * transform2;
        return result;
    }
}

// pathway from the flange to the device
// tmac has a base which is flange to tmac base, but the tool describes how to orient the device to the FRS so that orientation lines up. 
public class cEndEffectorFeature
{
    public cTransform Base;
    public cTransform Tool;

    public cEndEffectorFeature()
    {
        Base = new cTransform();
        Tool = new cTransform();
    }

    public cEndEffectorFeature(cTransform baseTransform, cTransform toolTransform)
    {
        Base = baseTransform;
        Tool = toolTransform;
    }
}

public class TransformData
{
    private static TransformData? instance;
    private static readonly object lockObject = new object();

    public cTransform instrumentToWorld { get; set; }

    public cEndEffectorFeature tmac { get; set; }
    public cEndEffectorFeature boreas { get; set; }
    public cEndEffectorFeature smr1 { get; set; }
    public cEndEffectorFeature smr2 { get; set; }
    public cEndEffectorFeature smr3 { get; set; }
    public cEndEffectorFeature smr4 { get; set; }

    public cEndEffectorFeature activeTool { get; set; }
    public cEndEffectorFeature activePart { get; set; }

    private TransformData()
    {
        instrumentToWorld = new cTransform();

        tmac = new cEndEffectorFeature();
        boreas = new cEndEffectorFeature();
        smr1 = new cEndEffectorFeature();
        smr2 = new cEndEffectorFeature();
        smr3 = new cEndEffectorFeature();
        smr4 = new cEndEffectorFeature();

        activeTool = new cEndEffectorFeature();
        activePart = new cEndEffectorFeature();
    }

    public static TransformData Instance
    {
        get
        {
            if (instance == null)
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = new TransformData();
                    }
                }
            }
            return instance;
        }
    }
}
