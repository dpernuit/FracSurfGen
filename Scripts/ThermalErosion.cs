using UnityEngine;
using System.Collections;

public class ThermalErosion : MonoBehaviour
{
    // Slope difference in %of the ZRange
    public  float   m_fThresholdPercent = 0.25f;

    // Slope value calculated from the range
    private float   m_fThresholdValue = 0.0f;

    // Number of erosion iterations to aplly on the buffer
    public int m_nIterations = 10;

    // Point buffer
    public float[] m_tBuffer;

    // Buffer Size
    public int m_nXSize = 512;
    public int m_nYSize = 512;


	// Use this for initialization
	void Start ()
    {
        m_tBuffer = new float[m_nXSize * m_nYSize];
    }
	
	// Update is called once per frame
	void Update ()
    {
	
	}



    //
    // Erode
    //
    //  Erodes the buffer m_nIterations times
    //
    void Erode()
    {
        // First we want to calculate the Threshold value corresponding to the percent threshold
        float fMin = m_tBuffer[0];
        float fMax = fMin;
        for(int n = 1; n < (m_nXSize * m_nYSize); n++)
        {
            if (m_tBuffer[n] > fMax)
                fMax = m_tBuffer[n];
            else if (m_tBuffer[n] < fMin)
                fMin = m_tBuffer[n];
        }

        // Value = Range * Percent
        m_fThresholdValue = (fMax - fMin) * m_fThresholdPercent;

        // Erodes the buffer
        for (int i = 0; i < m_nIterations; i++)
        {
            ErodeOnce();
        }
    }



    //
    // ErodeOnce
    //
    //  Applies a Thermal Erosion once for every point in the buffer
    //
    void ErodeOnce()
    {
        for (int nY = 0; nY < m_nYSize; nY++)
        {
            for (int nX = 0; nX < m_nXSize; nX++)
            {
                ErodeOnePoint(nX, nY);
            }
        }
    }



    //
    // ErodeOnePoint
    //
    //  Applies thermal erosion to the 8-Neighbours of the current point:
    //  if the current point is higher than its neighbour, and that the difference 
    //  is higher than the threshold, some material from the current point will be distributed
    //  to its neighbour
    //
    //  For example, with a threshold of 0.25, we'll have:
    //
    //      0.0 0.0 1.0       0.075 0.075 0.8       
    //      0.0 1.0 2.0   >   0.075 0.635 0.8
    //      0.0 0.0 0.8       0.075 0.075 0.8
    //
    void ErodeOnePoint(int nX, int nY)
    {
        // We need to store the total, max and all the height differences for the neighbourhood
        float fMaxDiff = 0.0f;
        float fTotalDiff = 0.0f;
        float[] tNeighbourDiff =  { 0, 0, 0,
                                    0, 0, 0,
                                    0, 0, 0};

        // Visit the neighbourhood and check for height differences
        int n = 0;
        for (int nNeighbY = Mathf.Max(0, nY - 1); nNeighbY <= Mathf.Min(nY + 1, m_nYSize - 1); nNeighbY++)
        {
            for (int nNeighbX = Mathf.Max(0, nX - 1); nNeighbX <= Mathf.Min(nX + 1, m_nXSize - 1); nNeighbX++)
            {
                // Skip the current point
                if ((nNeighbX == nX) && (nNeighbY == nY))
                {
                    n++;
                    continue;
                }                            

                float fDiff = m_tBuffer[nX + nY * m_nXSize] - m_tBuffer[nNeighbX + nNeighbY * m_nXSize];

                // If the inclination is bigger than the threshold,
                // We need to "break" some of the material
                if (fDiff > m_fThresholdValue)
                {
                    tNeighbourDiff[n] = fDiff;
                    fTotalDiff += fDiff;

                    if (fDiff > fMaxDiff)
                        fMaxDiff = fDiff;
                }
            }
        }

        // Distribute some of the broken material to the neighbour
        if (fTotalDiff > 0.0f)
        {
            // Visit the neighbourhood
            n = 0;
            for (int nNeighbY = Mathf.Max(0, nY - 1); nNeighbY <= Mathf.Min(nY + 1, m_nYSize - 1); nNeighbY++)
            {
                for (int nNeighbX = Mathf.Max(0, nX - 1); nNeighbX <= Mathf.Min(nX + 1, m_nXSize - 1); nNeighbX++)
                {
                    // Skip the current point
                    if ((nNeighbX == nX) && (nNeighbY == nY))
                    {
                        //n++;
                        //continue;

                        // Instead of skipping the current point, we remve the total quantity of material that's going to be removed
                        m_tBuffer[nX + nY * m_nXSize] -= 0.5f * (fMaxDiff - m_fThresholdValue);
                    }

                    // Calculate the quantity of material to distribute to the neighbour
                    float fDistribute = 0.5f * (fMaxDiff - m_fThresholdValue) * (tNeighbourDiff[n] / fTotalDiff);

                    // Remove it from the current point
                    //m_tBuffer[nX + nY * m_nXSize] -= fDistribute;

                    // then add it to the neighbour
                    m_tBuffer[nNeighbX + nNeighbY * m_nXSize] += fDistribute;
                }
            }
        }

        // Erode finish!
    }

}
