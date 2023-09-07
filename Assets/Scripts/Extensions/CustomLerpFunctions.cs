using UnityEngine;

namespace Extensions
{
    public static class CustomLerpFunctions
    {
        public static Vector3 Slerp(Vector3 startPoint, Vector3 endPoint, float t)
        {
            float startRadius = startPoint.magnitude;
            float endRadius = endPoint.magnitude;

            float startTheta = Mathf.Acos(startPoint.y / startRadius);
            float endTheta = Mathf.Acos(endPoint.y / endRadius);

            float startPhi = Mathf.Atan2(startPoint.z, startPoint.x);
            float endPhi = Mathf.Atan2(endPoint.z, endPoint.x);

            float radius = Mathf.Lerp(startRadius, endRadius, t);
            float theta = Mathf.Lerp(startTheta, endTheta, t);
            float phi = Mathf.Lerp(startPhi, endPhi, t);

            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);
            float sinPhi = Mathf.Sin(phi);
            float cosPhi = Mathf.Cos(phi);

            float x = radius * sinTheta * cosPhi;
            float y = radius * cosTheta;
            float z = radius * sinTheta * sinPhi;

            return new Vector3(x, y, z);
        }
    }
}
