using UnityEngine;

namespace _01_Scripts.Third_Person_Controller
{
    public class EnvironmentScanner : MonoBehaviour
    {
        //[SerializeField] Vector3 forwardRayOffset = new Vector3(0, 2.5f, 0);
        [SerializeField] float heightRayLength = 4;

        [SerializeField] float obstacleCheckRange = 0.7f;

        public float ledgeHeightThreshold = 1f;

        [field: SerializeField] public LayerMask ObstacleLayer { get; set; } = 1;
        [field: SerializeField] public LayerMask LedgeLayer { get; set; } = 1;


        public ObstacleHitData ObstacleCheck(bool performHeightCheck = true, float forwardOriginOffset = 1f)
        {
            var hitData = new ObstacleHitData();

            var forwardOrigin = transform.position + Vector3.up * forwardOriginOffset;
            hitData.ForwardHitFound = Physics.BoxCast(forwardOrigin, new Vector3(0.1f, 0.7f, 0.01f), transform.forward,
                out hitData.ForwardHit, Quaternion.LookRotation(transform.forward), obstacleCheckRange, ObstacleLayer);

            //Debug.DrawRay(forwardOrigin, transform.forward * obstacleCheckRange, (hitData.forwardHitFound) ? Color.red : Color.white);
            //BoxCastDebug.DrawBoxCastBox(forwardOrigin, new Vector3(0.1f, 0.7f, 0.01f), Quaternion.LookRotation(transform.forward), transform.forward, obstacleCheckRange, Color.green);


            //DrawAxis(hitData.forwardHit.point, 0.1f, Color.blue);

            if (hitData.ForwardHitFound && performHeightCheck)
            {
                var heightOrigin = hitData.ForwardHit.point + Vector3.up * heightRayLength;

                //GizmosExtend.drawSphereCast(hitData.forwardHit.point, 0.2f, Vector3.up, heightRayLength, Color.black);
                //var spaceCheckOrigin = transform.position + Vector3.up;
                //if (Physics.Raycast(spaceCheckOrigin, Vector3.up, out hitData.heightHit, heightRayLength, ObstacleLayer) && hitData.heightHit.point.y > hitData.forwardHit.point.y)
                //    heightOrigin.y = hitData.heightHit.point.y;
                var spaceCheckOrigin = transform.position + Vector3.up * heightRayLength;
                spaceCheckOrigin.y = heightOrigin.y;
                if (Physics.Raycast(spaceCheckOrigin, Vector3.down, out hitData.HeightHit, heightRayLength, ObstacleLayer) && hitData.HeightHit.point.y > hitData.ForwardHit.point.y)
                    heightOrigin.y = hitData.HeightHit.point.y;

                for (int i = 0; i < 4; ++i)
                {
                    //hitData.heightHitFound = Physics.BoxCast(heightOrigin, obstacleBoxCastHalf, Vector3.down, out hitData.heightHit,
                    //    Quaternion.LookRotation(Vector3.down), heightRayLength, ObstacleLayer);
                    //BoxCastDebug.DrawBoxCastBox(heightOrigin, obstacleBoxCastHalf,
                    //    Quaternion.LookRotation(Vector3.down), Vector3.down, heightRayLength, Color.green);

                    hitData.HeightHitFound = Physics.SphereCast(heightOrigin, 0.2f, Vector3.down, out hitData.HeightHit, heightRayLength, ObstacleLayer);

                    //GizmosExtend.drawSphereCast(heightOrigin, 0.2f, Vector3.down, heightRayLength, Color.magenta);


                    if (hitData.HeightHitFound && Vector3.Angle(Vector3.up, hitData.HeightHit.normal) < 45f)
                        break;

                    hitData.HeightHitFound = false;
                    heightOrigin += transform.forward * 0.15f;
                }
                // Debug.DrawRay(hitData.heightHit.point, hitData.heightHit.normal, Color.blue);

                if (hitData.HeightHitFound)
                {
                    float length = 0.8f;
                    //forwardOrigin = transform.position + transform.forward * length / 2; ;
                    forwardOrigin = hitData.HeightHit.point;
                    forwardOrigin.y = hitData.HeightHit.point.y + 0.2f + length * 1.5f / 2;
                    hitData.HasSpace = !Physics.CheckBox(forwardOrigin, new Vector3(0.5f, 1.5f, 0.7f) * length / 2, Quaternion.LookRotation(transform.forward), ObstacleLayer);

                    //BoxCastDebug.DrawBoxCastBox(forwardOrigin, new Vector3(0.5f, 1.5f, 0.7f) * length / 2, Quaternion.LookRotation(transform.forward), transform.forward, 0f, Color.green);

                    var spaceOrigin = hitData.HeightHit.point + transform.forward * 0.5f + Vector3.up * 0.6f;

                    hitData.HasSpaceToVault = Physics.SphereCast(spaceOrigin, 0.1f, Vector3.down, out _, 1f, ObstacleLayer);
                    //GizmosExtend.drawSphereCast(spaceOrigin, 0.1f, Vector3.down, 1f, Color.magenta);


                    var dir = hitData.HeightHit.point - transform.position;
                    dir.y = 0;
                    heightOrigin = hitData.HeightHit.point;
                    heightOrigin.y += 0.8f;
                    hitData.LedgeHit = hitData.HeightHit;
                    int i = 1;
                    for (; i <= 6; ++i)
                    {
                        var ledgeHitFound = Physics.CheckSphere(heightOrigin, 0.4f, ObstacleLayer);

                        if (!ledgeHitFound)
                        {
                            ledgeHitFound = Physics.SphereCast(heightOrigin, 0.3f, Vector3.down, out RaycastHit ledgeHit, 2f, ObstacleLayer);

                            if (ledgeHitFound && Mathf.Abs(ledgeHit.point.y - hitData.HeightHit.point.y) < 0.4f)
                            {
                                hitData.LedgeHit = ledgeHit;
                                hitData.LedgeHitFound = true;
                            }
                            else
                                break;

                            heightOrigin += transform.forward * 0.4f;
                        }
                        else
                        {
                            hitData.LedgeHitFound = false;
                            break;
                        }
                    }
                    if (hitData.LedgeHitFound) 
                        hitData.LedgeHitFound = !Physics.CheckSphere(hitData.LedgeHit.point + Vector3.up * 0.5f, 0.3f, ObstacleLayer) && i < 7;
                }

            }

            return hitData;
        }
    }
    public struct ObstacleHitData
    {
        public bool ForwardHitFound;
        public bool HeightHitFound;
        public bool LedgeHitFound;
        public RaycastHit ForwardHit;
        public RaycastHit HeightHit;
        public RaycastHit LedgeHit;
        public bool HasSpaceToVault;
        public bool HasSpace;
    }
}

