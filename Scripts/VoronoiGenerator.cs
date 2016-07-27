using UnityEngine;

/**
 * 
 * VoronoiGenerator
 * 
 *  Simple Voronoi Diagram Generator.
 *  
 *      Generates a Voronoi Diagram
 *      
 *      The Diagram uses a maximum of 3 parameters c1, c2 and c3.
 *      Heights are calculated by
 *      
 *          H = c1 * d1 + c2 * d2 + c3 * d3
 *          
 *      Where d1 is the minimum distance to a feature, d2 the second smaller distance and d3 the third.
 *      
 *      Press "Space" to generate a new diagram
 *      or "R" to rebuild the same diagram with new parameters (use the inspector)
 *      
 *      Attach the script to a quad to display the diagram
 *
 */
public class VoronoiGenerator : MonoBehaviour
{
    // Number of voronoi feature points to be generated
    public int m_nNumberOfFeatures = 128;

    // Size of the resulting diagram
    public int m_nXSize = 512;
    public int m_nYSize = 512;

    // Voronoi paramaters
    public float m_fC1 = -1.0f;
    public float m_fC2 = 1.0f;
    public float m_fC3 = 0.0f;
    //public float m_fC4 = 1.0f;

    // All the featurePoints Position
    private Vector2[] m_tFeaturePoints;

    // Resulting Array
    private float[] m_tVoronoiBuffer;

    // Needs recreate?
    private bool m_bNeedsRecreate = true;

    // Random Number Generator
    private System.Random m_RNG;
    // Seed used by the RNG
    private int m_nSeed;
    

    // Use this for initialization
    void Start()
    {
        m_bNeedsRecreate = true;

        // Creates a new RNG
        m_nSeed = System.DateTime.Now.GetHashCode();
        m_RNG = new System.Random(m_nSeed);
    }
	
	// Update is called once per frame
	void Update ()
    {
        // SpaceBar will recreate the Mesh
        if (Input.GetButtonDown("Jump"))
        {
            // Creates a new RNG
            m_nSeed = System.DateTime.Now.GetHashCode();
            m_RNG = new System.Random(m_nSeed);

            // GetButtonDown is only called once
            m_bNeedsRecreate = true;
        }

        // R will force rebuild the Mesh with the same parameters
        if (Input.GetKeyDown(KeyCode.R))
        {
            // GetButtonDown is only called once
            m_bNeedsRecreate = true;
        }

            if (m_bNeedsRecreate)
            Generate();
	}

    //
    // FracRandNum
    //
    //  Returns a RandomNumber between (0, fMax)
    //
    private float RandNum(float fMax)
    {
        return (float)(m_RNG.NextDouble() * fMax);
    }


    void Generate()
    {
        // resets the random generator to the current Seed
        m_RNG = new System.Random(m_nSeed);

        // 1. Creates Random feature points
        GenerateFeaturePoints();

        // 2. Fills the buffer with the Voronoi Diagram
        GenerateVoronoi();

        // 3. Applies the material to the Quad
        VoronoiToTexture();

        m_bNeedsRecreate = false;
    }


    // Random places features on the buffer
    void GenerateFeaturePoints()
    {
        if (m_nNumberOfFeatures < 1)
            return;

        // Initialises the Array
        m_tFeaturePoints = new Vector2[m_nNumberOfFeatures];

        for (int n = 0; n < m_nNumberOfFeatures; n++)
        {
            // Generates Random feature
            m_tFeaturePoints[n].x = RandNum(m_nXSize-1);
            m_tFeaturePoints[n].y = RandNum(m_nYSize-1);
        }
    }


    // Calculates the Voronoi Diagram for the given feature set
    void GenerateVoronoi()
    {
        // Allocates the array
        m_tVoronoiBuffer = new float[m_nXSize * m_nYSize];

        // For each point in the array
        for(int nY = 0; nY < m_nYSize; nY++)
        {
            for(int nX = 0; nX < m_nXSize; nX++)
            {
                // Get the two closest distance
                float fD1 = 0.0f, fD2 = 0.0f, fD3 = 0.0f;
                GetClosestFeatureDistances(nX, nY, ref fD1, ref fD2, ref fD3);

                // Calculate the voronoi value c1d1 + c2d2
                m_tVoronoiBuffer[nX + nY * m_nXSize] = m_fC1 * fD1 + m_fC2 * fD2 + m_fC3 * fD3;
            }
        }
    }


    // Return the 3 smallest Distance for the current point
    void GetClosestFeatureDistances(int nX, int nY, ref float fD1, ref float fD2, ref float fD3)
    {
        // Initialisation
        Vector2 vPos = new Vector2(nX, nY);
        fD1 = GetSqrDistanceToFeature(vPos, 0);
        fD2 = fD1;
        fD3 = fD1;

        // Get the two minimum distances
        for(int n = 1; n < m_nNumberOfFeatures; n++)
        {
            float fDist = GetSqrDistanceToFeature(vPos, n);
            if (fDist < 0.0f)
                continue;

            if (fDist > fD3)
                continue;

            if (fDist < fD1)
            {
                fD3 = fD2;
                fD2 = fD1;
                fD1 = fDist;
            }
            else if (fDist < fD2)
            {
                fD3 = fD2;
                fD2 = fDist;
            }
            else
            {
                fD3 = fDist;
            }
        }

        // We used squared distance, so we need to sqrt them
        //fD1 = Mathf.Sqrt(fD1);
        //fD2 = Mathf.Sqrt(fD2);
    }


    // Calculates the Square Distance to a feature of given index
    float GetSqrDistanceToFeature(Vector2 vPos, int nFeatureID)
    {
        if((nFeatureID < 0) || (nFeatureID > m_nNumberOfFeatures))
                return -1.0f;

        Vector2 vDistance = m_tFeaturePoints[nFeatureID] - vPos;
        return vDistance.SqrMagnitude();
    }

    // Simple conversion of the buffer to a Texture
    private void VoronoiToTexture()
    {
        // Max value = max distance
        float fMax = m_tVoronoiBuffer[0];
        float fMin = m_tVoronoiBuffer[0];
        for (int n = 0;  n < m_tVoronoiBuffer.Length; n++)
        {
            if (m_tVoronoiBuffer[n] > fMax)
                fMax = m_tVoronoiBuffer[n];
            else if (m_tVoronoiBuffer[n] < fMin)
                fMin = m_tVoronoiBuffer[n];
        }

        // If thresholding, only positive values are taken into account
        //if (m_bThreshold)
        //    fMin = 0.0f;

        // Creates a texture
        Texture2D texture = new Texture2D(m_nXSize, m_nYSize);
        // For each point in the array
        for (int nY = 0; nY < m_nYSize; nY++)
        {
            for (int nX = 0; nX < m_nXSize; nX++)
            {
                float fCurrentValue = (m_tVoronoiBuffer[nX + nY * m_nXSize] - fMin) / (fMax - fMin);
                texture.SetPixel(nX, nY, new Color(fCurrentValue, fCurrentValue, fCurrentValue));
            }
        }

        texture.Apply();

        GetComponent<Renderer>().material.mainTexture = texture;
    }
}
