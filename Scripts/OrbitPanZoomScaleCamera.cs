using UnityEngine;

/**
 * 
 * OrbitPanZoomScaleCamera
 * 
 *  Script that allows the camera to Orbit/Pan/Zoom
 *  Can also scale an object along its Z Axis
 *  
 *  LeftClick / Fire1 to rotate around the object
 *  RightClick / Fire2 to pan the camera
 *  MouseWheel to zoom in/out
 *  
 *  hold Shift/Fire3 to Z-Scale the object
 *
 */
public class OrbitPanZoomScaleCamera : MonoBehaviour
{
    // The Target of the camera
    // The camera will always look at it, and will scale it
    public Transform m_Target;

    // Object that will be scaled when shift-clicking
    public Transform m_ObjectToScale;

    // Rotating speeds
    public float m_fXRotateSpeed = 5.0f;
    public float m_fYRotateSpeed = 5.0f;

    // Zooming speed
    public float m_fZoomSpeed = 50.0f;

    // ZoomSmoothing 
    // Lower values will dampen the zooming effect
    public float m_fZoomSmoothing = 8.0f;

    public float m_fPanSpeed = 5.0f;

    public float m_fScaleSpeed = 5.0f;

    // Current & desired distance from the Target
    private float m_fCurrentDistance = 150.0f;
    private float m_fDesiredDistance = 150.0f;

    // Vector used to memorize the last mouse position for the scaling
    private Vector2 m_vOldMousePos;

    // The current rotation degree
    private float m_fXAngleInDegree = 45.0f;
    private float m_fYAngleInDegree = 45.0f;

    // Use this for initialization
    void Start ()
    {
	
	}

    // LateUpdate is called once per frame, after Update has finished
    void LateUpdate ()
    {
        // A target is required
        if (m_Target == null)
            return;

        // Get the state of the inputs
        bool bFire1 = Input.GetButton("Fire1");
        bool bFire2 = Input.GetButton("Fire2");        
        bool bShift = Input.GetButton("Fire3");
        bool bWheel = (Input.GetAxis("Mouse ScrollWheel") != 0.0f);

        // We need to memorize the mouse position when the user start scaling
        // GetButtonDown is true only for the first frame.
        if (Input.GetButtonDown("Fire1") || Input.GetButtonDown("Fire3"))
            m_vOldMousePos = Input.mousePosition;

        // When left clicking, we either scale the object or rotate around it
        if (bFire1)
        {            
            if(bShift)
            {
                // Scaling
                DoScaling();
            }               
            else
            {
                // Rotate
                m_fXAngleInDegree += Input.GetAxis("Mouse X") * m_fXRotateSpeed;
                m_fYAngleInDegree -= Input.GetAxis("Mouse Y") * m_fYRotateSpeed;
            }
        }

        // Panning
        if (bFire2)
        {
            float fXPan = Input.GetAxis("Mouse X") * m_fPanSpeed;
            float fYPan = Input.GetAxis("Mouse Y") * m_fPanSpeed;

            if((fXPan != 0.0f) && (fYPan != 0.0f))
            {
                // The Target's Position must be modified in the local (camera) space 
                m_Target.rotation = transform.rotation;
                m_Target.Translate(Vector3.right * -fXPan);
                m_Target.Translate(transform.up * -fYPan, Space.World);
            }
        }

        // Zoom
        if (bWheel)
        {            
            // To smoothen the zoom, the mouseWheel modified the desired distance
            // We will interpolate to that distance after
            m_fDesiredDistance -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * m_fZoomSpeed * Mathf.Abs(m_fDesiredDistance);
        }

        // Get the Quaternion corresponding to the camera's orientation
        Quaternion qRotation = Quaternion.Euler(m_fYAngleInDegree, m_fXAngleInDegree, 0);

        // To get a smoother zoom effect, we use lerp to interpolate to the desired distance
        m_fCurrentDistance = Mathf.Lerp(m_fCurrentDistance, m_fDesiredDistance, Time.deltaTime * m_fZoomSmoothing);

        // Calculates the new camera position
        Vector3 vPosition = m_Target.position - (qRotation * Vector3.forward * m_fCurrentDistance);

        // Apply the new position/rotation to the camera's Transform
        transform.rotation = qRotation;
        transform.position = vPosition;
    }


    // Scales the "ObjectToScale" along its ZAxis
    private void DoScaling()
    {
        if (m_ObjectToScale == null)
            return;

        Vector2 vCurrentMousePos = Input.mousePosition;
        float fDiff = vCurrentMousePos.y - m_vOldMousePos.y;
        if (fDiff == 0.0f)
            return;

        // Get the current object's scale and modify it
        Vector3 vCurrentScale = m_ObjectToScale.transform.localScale;
        vCurrentScale.z += m_fScaleSpeed * (fDiff / 1000.0f);

        // Limit the min/max scale values
        vCurrentScale.z = (vCurrentScale.z > 0) ? vCurrentScale.z : 1e-3f;
        vCurrentScale.z = (vCurrentScale.z < 1) ? vCurrentScale.z : 1;

        // Apply the new scale to the object
        m_ObjectToScale.transform.localScale = vCurrentScale;

        m_vOldMousePos = vCurrentMousePos;
    }
}
