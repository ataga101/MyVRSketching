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
        var b = Vector<float>.Build.Dense(3 * N);

        var normal = calcFitPlane();

        Matrix<float> NN = normal.ToColumnMatrix() * normal.ToRowMatrix();


        float factor = 2f / (N - 1);

        var tangents = new List<Vector<float>>();

        for(int i=0; i<N-1; i++)
        {
            var diff = ControlPoints[i + 1] - ControlPoints[i];
            tangents.Add(Vector<float>.Build.Dense(new float[] { diff.x, diff.y, diff.z }));
        }

        for (int k = 0; k < N; k++)
        {
            Vector<float> b_k = Vector<float>.Build.Dense(3);

            if (k < N - 1 && !Mathf.Approximately(tNorms[k], 0f))
                b_k += factor * NN * tangents[k] / tNorms[k];
            if (k > 0 && !Mathf.Approximately(tNorms[k - 1], 0f))
                b_k += -factor * NN * tangents[k - 1] / tNorms[k - 1];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (k < N - 1)
                    {
                        A[3 * k + i, 3 * k + j] += factor * NN[i, j] / tNorms[k];
                        A[3 * k + i, 3 * (k + 1) + j] += -factor * NN[i, j] / tNorms[k];
                    }
                    if (k > 0)
                    {
                        A[3 * k + i, 3 * k + j] += factor * NN[i, j] / tNorms[k - 1];
                        A[3 * k + i, 3 * (k - 1) + j] += -factor * NN[i, j] / tNorms[k - 1];
                    }
                }
                b[3 * k + i] = b_k[i];
            }
            
        }

        return (A, b);
    }

    private (Matrix<float>, Vector<float>) EtangentMat(Vector3 Ttarget, int bezierIndex)
    {
        Matrix<float> A = Matrix<float>.Build.Dense(3 * N, 3 * N);
        Vector<float> b = Vector<float>.Build.Dense(3 * N);

        int bStart, bEnd;
        if (bezierIndex < N - 1)
        {
            bStart = bezierIndex;
            bEnd = bezierIndex + 1;
        }
        else
        {
            bStart = bezierIndex - 1;
            bEnd = bezierIndex;
        }

        Vector3 tangent = ControlPoints[bEnd] - ControlPoints[bStart];

        var crossTtarget = Matrix<float>.Build.Dense(3, 3);
        crossTtarget[0, 1] = -Ttarget.z;
        crossTtarget[0, 2] = Ttarget.y;
        crossTtarget[1, 0] = Ttarget.z;
        crossTtarget[1, 2] = -Ttarget.x;
        crossTtarget[2, 0] = -Ttarget.y;
        crossTtarget[2, 1] = Ttarget.x;

        var f = 2f / Mathf.Max(eps, Vector3.Dot(tangent, tangent));
        var targetCrossTangent = -f * crossTtarget * Vector<float>.Build.Dense(new float[] {tangent.x, tangent.y, tangent.z });

        for(int i=0; i<3; i++)
        {
            for(int j=0; j<3; j++)
            {
                A[3 * bStart + i, 3 * bStart + j] = -f * crossTtarget[i, j];
                A[3 * bStart + i, 3 * bEnd + j] = f * crossTtarget[i, j];
                A[3 * bEnd + i, 3 * bStart + j] = f * crossTtarget[i, j];
                A[3 * bEnd + i, 3 * bEnd + j] = -f * crossTtarget[i, j];
            }

            b[3 * bStart + i] = targetCrossTangent[i];
            b[3 * bEnd + i] = -targetCrossTangent[i];
        }


        return (A, b);
    }
}
