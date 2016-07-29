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

    // Ignores the 3rd parameters
    public bool m_nUseOnly2Parameters = true;

    // Perturbation intensity value (as % of size)
    public float m_fPerturbationPercent;

    // Number of zone for feature genearation
    // if 0 or 1, all the features will be randomly place
    // else the features will be spread across NxN zones
    public int  m_nFeatureZone = 0;

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
    // RandNum
    //
    //  Returns a RandomNumber between (0, fMax)
    //
    private float RandNum(float fMax)
    {
        return (float)(m_RNG.NextDouble() * fMax);
    }


    //
    // RandNumMirror
    //
    //  Returns a RandomNumber between (-fVal, fVal)
    //
    private float RandNumMirror(float fVal)
    {
        return (float)((m_RNG.NextDouble() * 2.0) - 1.0) * fVal;
    }

    void Generate()
    {
        // resets the random generator to the current Seed
        m_RNG = new System.Random(m_nSeed);

        // 1. Creates Random feature points
        GenerateFeaturePoints();

        // 2. Fills the buffer with the Voronoi Diagram
        GenerateVoronoi();

        // 3. Applies a perturbation filtering to the voronoi diagram
        ApplyPerturbationFiltering();

        // 4. Applies the material to the Quad
        VoronoiToTexture();

        m_bNeedsRecreate = false;
    }

    // Random places features on the buffer
    void GenerateFeaturePoints()
    {
        if (m_nNumberOfFeatures < 1)
            return;

        if ((m_nXSize < 2) || (m_nYSize < 2))
            return;

        // Initialises the Array
        m_tFeaturePoints = new Vector2[m_nNumberOfFeatures];

        if(m_nFeatureZone < 2)
        {
            // Generates Random features
            for (int n = 0; n < m_nNumberOfFeatures; n++)
            {   
                m_tFeaturePoints[n].x = RandNum(m_nXSize - 1);
                m_tFeaturePoints[n].y = RandNum(m_nYSize - 1);
            }
        }
        else
        {
            // The random features will be spread across zones
            float fZoneXSize = m_nXSize / m_nFeatureZone;
            float fZoneYSize = m_nYSize / m_nFeatureZone;

            // Calculates the theoretical number of feature per Zone
            float fFeaturePerZone = m_nNumberOfFeatures / (m_nFeatureZone * m_nFeatureZone);

            // After this limit, we have to move to the next zone
            int nNextZoneLimit = Mathf.CeilToInt(fFeaturePerZone);

            // Generates Random features according to a Zone size and spread them through all the zones
            int nCurrentZoneX = 0, nCurrentZoneY = 0;
            float fCurrentXOffset = 0.0f, fCurrentYOffset = 0.0f;
            for (int n = 0; n < m_nNumberOfFeatures; n++)
            {
                if(n > nNextZoneLimit)
                {
                    // Increase the limit
                    nNextZoneLimit += Mathf.FloorToInt(fFeaturePerZone);

                    nCurrentZoneX++;
                    if(nCurrentZoneX < m_nFeatureZone)
                    {
                        // Move to the next zone
                        fCurrentXOffset += fZoneXSize;
                    }
                    else
                    {
                        // Move to a new zone line
                        nCurrentZoneY++;
                        nCurrentZoneX = 0;

                        fCurrentXOffset = 0.0f;
                        fCurrentYOffset += fZoneYSize;
                    }
                }

                m_tFeaturePoints[n].x = fCurrentXOffset + RandNum(fZoneXSize);
                m_tFeaturePoints[n].y = fCurrentYOffset + RandNum(fZoneYSize);
            }
        }
    }


    // Calculates the Voronoi Diagram for the given feature set
    void GenerateVoronoi()
    {
        // Allocates the array
        m_tVoronoiBuffer = new float[m_nXSize * m_nYSize];

        if(m_nUseOnly2Parameters)
        {
            // For each point in the array
            for (int nY = 0; nY < m_nYSize; nY++)
            {
                for (int nX = 0; nX < m_nXSize; nX++)
                {
                    // Get the two closest distance

                    float fD1 = 0.0f, fD2 = 0.0f;
                    Get2ClosestFeatureDistances(nX, nY, ref fD1, ref fD2);

                    // Calculate the voronoi value c1d1 + c2d2
                    m_tVoronoiBuffer[nX + nY * m_nXSize] = m_fC1 * fD1 + m_fC2 * fD2;
                }
            }
        }
        else
        {
            // For each point in the array
            for (int nY = 0; nY < m_nYSize; nY++)
            {
                for (int nX = 0; nX < m_nXSize; nX++)
                {
                    // Get the two closest distance

                    float fD1 = 0.0f, fD2 = 0.0f, fD3 = 0.0f;
                    Get3ClosestFeatureDistances(nX, nY, ref fD1, ref fD2, ref fD3);

                    // Calculate the voronoi value c1d1 + c2d2
                    m_tVoronoiBuffer[nX + nY * m_nXSize] = m_fC1 * fD1 + m_fC2 * fD2 + m_fC3 * fD3;
                }
            }
        }        
    }


    void ApplyPerturbationFiltering()
    {
        if (m_fPerturbationPercent <= 0)
            return;

        // Calculates the maximum displacement in Pixel
        float fMaxPerturbationX = m_fPerturbationPercent * m_nXSize;
        float fMaxPerturbationY = m_fPerturbationPercent * m_nYSize;

        // We need to copy the current buffer to apply the perturbation filter
        float[] tBufferCopy = new float[m_nXSize * m_nYSize];
        System.Array.Copy(m_tVoronoiBuffer, tBufferCopy, m_nXSize * m_nYSize);

        // 
        for(int nY = 0; nY < m_nYSize; nY++)
        {
            for(int nX = 0; nX < m_nXSize; nX++)
            {
                // Use the perturbation factor to calculate a new X/Y
                int nNewX = (int)(nX + RandNumMirror(fMaxPerturbationX));
                int nNewY = (int)(nY + RandNumMirror(fMaxPerturbationY));

                nNewX = Mathf.Clamp(nNewX, 0, m_nXSize - 1);
                nNewY = Mathf.Clamp(nNewY, 0, m_nYSize - 1);

                // Copy the data from the (newX, newY) to (nX, nY)
                m_tVoronoiBuffer[nX + nY * m_nXSize] = tBufferCopy[nNewX + nNewY * m_nXSize];
            }
        }
    }


    // Return the 2 smallest distance for the current point
    void Get2ClosestFeatureDistances(int nX, int nY, ref float fD1, ref float fD2)
    {
        // Initialisation
        Vector2 vPos = new Vector2(nX, nY);
        fD1 = GetSqrDistanceToFeature(vPos, 0);
        fD2 = fD1;

        // Get the two minimum distances
        for (int n = 1; n < m_nNumberOfFeatures; n++)
        {
            float fDist = GetSqrDistanceToFeature(vPos, n);
            if ((fDist < 0.0f) || (fDist > fD2))
                continue;

            if (fDist < fD1)
            {
                fD2 = fD1;
                fD1 = fDist;
            }
            else
            {
                fD2 = fDist;
            }
        }
    }

    // Return the 3 smallest Distance for the current point
    void Get3ClosestFeatureDistances(int nX, int nY, ref float fD1, ref float fD2, ref float fD3)
    {
        // Initialisation
        Vector2 vPos = new Vector2(nX, nY);
        fD1 = GetSqrDistanceToFeature(vPos, 0);
        fD2 = fD1;
        fD3 = fD1;

        // Get the three minimum distances
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

        // Creates a texture
        Texture2D texture = new Texture2D(m_nXSize, m_nYSize);
        // For each point in the array
        for (int nY = 0; nY < m_nYSize; nY++)
        {
            for (int nX = 0; nX < m_nXSize; nX++)
            {
                // Calculates the value from [min, max] to [0, 1]
                float fCurrentValue = (m_tVoronoiBuffer[nX + nY * m_nXSize] - fMin) / (fMax - fMin);
                texture.SetPixel(nX, nY, new Color(fCurrentValue, fCurrentValue, fCurrentValue));
            }
        }

        // Apply the modifications made by the setPixel calls to the texture
        texture.Apply();

        // Apply the texture to the current render
        if(GetComponent<Renderer>() != null)
            GetComponent<Renderer>().material.mainTexture = texture;
    }
}
