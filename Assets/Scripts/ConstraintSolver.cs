using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;

public class ConstraintSolver
{
    int numSegments;
    int N;

    PolyBezier pb;
    List<Vector3> ControlPoints;
    Vector3 ptarget1;
    Vector3 ptarget2;
    int targetType;
    float[] tNorms;

    float eps = 10e-6f;

    public ConstraintSolver(PolyBezier pb, Vector3 ptarget1, Vector3 ptarget2, int targetType)
    {
        //number of bezier curve
        this.numSegments = pb.numSegments;
        this.N = numSegments * 3 + 1;
        ControlPoints = new List<Vector3>();
        for(int i=0; i<numSegments; i++)
        {
            ControlPoints.Add(pb.beziers[i].P0);
            ControlPoints.Add(pb.beziers[i].P1);
            ControlPoints.Add(pb.beziers[i].P2);
        }
        ControlPoints.Add(pb.beziers[numSegments - 1].P3);
        this.pb = pb;
        this.ptarget1 = ptarget1;
        this.ptarget2 = ptarget2;
        this.targetType = targetType;
    }

    private (Matrix<float>, Vector<float>) EfidelMat(float displacement_normalizer)
    {
        float pFactor = 0.5f / N;
        float tFactor = 0.5f / (N - 1);

        tNorms = new float[N-1];

        for(int i=0; i<N-1; i++)
        {
            var Tangent = ControlPoints[i] - ControlPoints[i + 1];
            tNorms[i] = Vector3.Dot(Tangent, Tangent);
        }

        pFactor /= (displacement_normalizer * displacement_normalizer);

        Matrix<float> A = Matrix<float>.Build.Dense(3 * N, 3 * N);
        for(int i=0; i<N; i++)
        {
            for(int j=0; j<N; j++)
            {
                A[3 * i + j, 3 * i + j] += 2 * pFactor;
                if (i > 0 && tNorms[i-1] > this.eps)
                {
                    A[3 * i + j, 3 * i + j] += 2 * tFactor / tNorms[i - 1];
                    A[3 * i + j, 3 * (i - 1) + j] -= 2 * tFactor / tNorms[i - 1];
                }
                if (i < N-1 && tNorms[i] > this.eps)
                {
                    A[3 * i + j, 3 * i + j] += 2 * tFactor / tNorms[i];
                    A[3 * i + j, 3 * (i + 1) + j] -= 2 * tFactor / tNorms[i];
                }
            }
        }
        return (A, Vector<float>.Build.Dense(3 * N));
    }

    private Vector<float> calcFitPlane()
    {
        Vector3 avg = Vector3.zero;
        for(int i=0; i<N; i++)
        {
            avg += ControlPoints[i];
        }

        avg /= N;

        float xx = 0f; float xy = 0f; float xz = 0f;
        float yy = 0f; float yz = 0f; float zz = 0f;

        for(int i=0; i<N; i++)
        {
            Vector3 r = ControlPoints[i] - avg;
            xx += r.x * r.x;
            xy += r.x * r.y;
            xz += r.x * r.z;
            yy += r.y * r.y;
            yz += r.y * r.z;
            zz += r.z * r.z;
        }

        float det_x = yy * zz - yz * yz;
        float det_y = xx * zz - xz * xz;
        float det_z = xx * yy - xy * xy;

        float det_max = Mathf.Max(det_x, det_y, det_z);
        if(det_max <= 0f)
        {
            return Vector<float>.Build.Dense(3);
        }
        Vector3 normal;
        if (det_max == det_x)
        {
            normal = new Vector3(det_x, xz * yz - xy * zz, xy * yz - xz * yy);
        }
        else if(det_max == det_y)
        {
            normal = new Vector3(xz * yz - xy * zz, det_y, xy * xz - yz * xx);
        }
        else
        {
            normal = new Vector3(xy * yz - xz * yy, xy * xz - yz * xx, det_z);
        }

        normal = Vector3.Normalize(normal);

        return Vector<float>.Build.Dense(new float[] { normal.x, normal.y, normal.z });
    }

    private (Matrix<float>, Vector<float>) EPlainerMat()
    {
        var A = Matrix<float>.Build.Dense(3 * N, 3 * N);
        var B = Vector<float>.Build.Dense(3 * N);

        var normal = calcFitPlane();

        Matrix<float> NN = normal.ToColumnMatrix() * normal.ToRowMatrix();

        return (A, B);
    }
}
