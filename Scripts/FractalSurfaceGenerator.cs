using UnityEngine;
using System.Collections.Generic;

/**
 * 
 * FractalSurfaceGenerator
 * 
 *  Generates a procedural fractal surface using the Diamond-Square Algorithm.
 *  Its X and Y size will be (2^complexity)+n and it's fractal dimension (2+fractalDim).
 *  As unity cannot handle mesh bigger than 255 x 255, the result surface will 
 *  be splitted into multiple game object, but the final meshes will remain in a
 *  100x100x100 cube.
 *  
 *  Press Space to generate a new surface.
 *  
 *  Inspector parameters
 *  
 *      Complexity: Defines the number of iteration of the algorithm and the size
 *                  of the result.
 *                  
 *      FractalDim: The fractal dimension of the result. Smaller values will create 
 *                  smooth surfaces while higher will produces rougher surfaces
 *                  
 *      defaultMaterial: Material that will be applied to the result
 *                       Must be a 1D texture (vertical gradient) 
 *                       
 *      parentTransform: The resulting gameObject will be attach to it.
 *      
 *      
 *  The public functions SetFractalDimension() and SetComplexity() can be used
 *  by an UIElement to recreate the same mesh with different parameters
 *
 */
public class FractalSurfaceGenerator : MonoBehaviour
{
    // Complexity of the generated surface
    // Result will have a size of (2^n)+1 
    [Range(1, 10)]
    public int          m_nComplexity = 8;

    // Fractal Complexity used for the algorithm
    // The higher the spikyer
    // The lower the smoother
    // Result will have a fractal dimension of 2 + FractalDim
    [Range(0.01f, 1.0f)]
    public float        m_fFractalDim = 0.35f;

    // Default Material to apply to the generated Mesh
    public Material     m_DefaultMaterial;
    
    // Transform of the parent Object
    // The meshes generated will be attached to it 
    public Transform    m_ParentTransform;

    // Improved mesh
    // Instead of building the mesh's quads along the same diagonal
    // Adds a test to use the "best" diagonal to split the quad in 2
    public bool         m_bBetterMeshSplitting = true;

    // Size of the result mesh
    // For now, XSize is always equal to YSize, and is (2^complexity)+1
    private int         m_nXSize;
    private int         m_nYSize;

    // FloatBuffer that will store the generated meshes "Altitudes"
    private float[]     m_tBuffer;
    
    // List of all the GameObjects Created by the algorithm
    // As unity cannot handle meshes bigger than 255x255
    // The following list will store all the meshes needed to display the Buffer
    private List<GameObject>    m_tGameObjects;
    
    // Random Number Generator
    private System.Random       m_RNG;

    // Seed used by the RNG
    // Can only be modified by pressing "Space"
    private int         m_nSeed;

    // Avoid the abusive recreation of the mesh by the UI
    private bool        m_bMeshIsGenerating = false;
    private bool        m_bMeshNeedsRecreate = true;
    private float       m_fMeshTimer = 0.0f;

    // Initialisation
    void Start()
    {
        // Initialises the GO List and the HeightBuffer
        m_tGameObjects = new List<GameObject>();

        // Max Size is 2049
        // Allocates the buffer once, and reuses it at each generation to save allocations.
        m_tBuffer = new float[2049 * 2049];

        // Creating a new RNG
        m_nSeed = System.DateTime.Now.GetHashCode();
        m_RNG = new System.Random(m_nSeed);
        
        Generate();
    }

    // Update is called once per frame
    void Update()
    {
        // Timer for the mesh recreation used by the UI
        if(m_fMeshTimer != 0.0f)
        {
            m_fMeshTimer -= Time.deltaTime;
            if (m_fMeshTimer < 0.0f)
                m_fMeshTimer = 0.0f;
        }

        // SpaceBar will recreate the Mesh
        if (Input.GetButtonDown("Jump"))
        {
            // Creates a new RNG
            m_nSeed = System.DateTime.Now.GetHashCode();
            m_RNG = new System.Random(m_nSeed);

            // GetButtonDown is only called once
            m_bMeshNeedsRecreate = true;

            // Ignore the UI mesh timer
            m_fMeshTimer = 0.0f;
        }

        // R will force rebuild the Mesh with the same parameteres (for debug)
        if (Input.GetKeyDown(KeyCode.R))
        {
            // GetButtonDown is only called once
            m_bMeshNeedsRecreate = true;
            // Ignore the UI mesh timer
            m_fMeshTimer = 0.0f;
        }

        // Do we need to rebuild the mesh?
        // And is it possible to do it right now?
        if ((m_bMeshNeedsRecreate) && (!m_bMeshIsGenerating) && (m_fMeshTimer <= 0.0f))
        {
            // Destroy the previous mesh
            Clear();

            // Generate a new one
            Generate();
        }
    }

    //
    //  Clear
    //
    private void Clear()
    {
        m_bMeshIsGenerating = true;

        // Destroy all the game Objects from the list
        for (int n = 0; n < m_tGameObjects.Count; n++)
        {
            Destroy(m_tGameObjects[n]);
            m_tGameObjects[n] = null;
        }

        // Clears the lists
        m_tGameObjects.RemoveAll(delegate (GameObject o) { return o == null; });

        // Destroy the Heightbuffer
        // delete[] m_tHeightBuffer
        
        // Resets the buffer because we cant destroy it
        for (int n = 0; n < m_tBuffer.Length; n++)
            m_tBuffer[n] = 0;

        m_bMeshIsGenerating = false;
    }

    //
    //  Generate
    //
    //      Fills the float buffer with a new fractal surface
    //      Then generates all the gameObjects required to display it
    // 
    private void Generate()
    {
        m_bMeshIsGenerating = true;

        // resets the random generator to the current Seed
        m_RNG = new System.Random(m_nSeed);

        // 1. Generate the new fractal surface in the float buffer
        {
            // Calculates the Size from the complexity
            int nCurrentSize = (int)Mathf.Pow(2.0f, m_nComplexity) + 1;

            // 2049 is the max size!
            if (nCurrentSize > 2049)
                nCurrentSize = 2049;

            m_nXSize = nCurrentSize;
            m_nYSize = nCurrentSize;

            // Allocates the point buffer
            //m_tBuffer = new float[m_nXSize * m_nYSize];
            
            // We need to initialise the four corners of the buffer with random values
            // This will "seed" the algorithm by giving us our first square
            float fCoeff = Mathf.Pow(nCurrentSize, (1 - m_fFractalDim));
            SetAt(0, 0, fracRandNum(fCoeff));
            SetAt(0, nCurrentSize-1, fracRandNum(fCoeff));
            SetAt(nCurrentSize-1, 0, fracRandNum(fCoeff));
            SetAt(nCurrentSize-1, nCurrentSize-1, fracRandNum(fCoeff));

            // Uses the Diamond-Square algorithm to add ever-increasing details to the Buffer
            // until we cannot reduced the iteration size.  
            while (nCurrentSize > 1)
            {
                DoDiamondSquareStep(nCurrentSize);
                nCurrentSize /= 2;
            }
        }

        // 2. Generate GameObjects from the buffer
        {
            // We have to memorize and reset the parent scale
            // so that the children get the proper scale when attached..
            Vector3 vOldParentScale = m_ParentTransform.transform.localScale;
            m_ParentTransform.transform.localScale = Vector3.one;

            // Then we'll build all the meshes from the buffer
            BuildAllGameObjectsFromBuffer();

            // We can now restore the parent scale
            m_ParentTransform.transform.localScale = vOldParentScale;
        }

        m_bMeshIsGenerating = false;
        m_bMeshNeedsRecreate = false;
    }


    //
    //  BuildAllGameObjectsFromBuffer
    //
    //      Build all the objects required to represent the Buffer
    //      As Unity cannot handle meshes bigger than 255x255
    //      We need to split the buffer as multiple SubMeshes in a Grid
    // 
    private void BuildAllGameObjectsFromBuffer()
    {
        // Verify the HeightBuffer's Size
        int nNumberOfVertices = m_nXSize * m_nYSize;
        //if (m_tBuffer.Length != nNumberOfVertices)
        //    return;

        // As unity cannot support more than 255x255 vertices per mesh,
        // We may need to split the buffer to a grid
        int nGridXSize = m_nXSize > 200 ? 200 : m_nXSize;
        int nGridYSize = m_nYSize > 200 ? 200 : m_nYSize;

        // Vertices will be limited to [-50, +50] in X Y and Z
        float fXLength = 100.0f;
        float fYLength = 100.0f;

        float fXOffset = -(fXLength / 2.0f);
        float fYOffset = -(fYLength / 2.0f);

        // We need to calculate the ZMin/ZMax values
        float fZMin = m_tBuffer[0];
        float fZMax = m_tBuffer[0];
        for(int n = 0; n < nNumberOfVertices; n++)
        {
            if (m_tBuffer[n] < fZMin)
                fZMin = m_tBuffer[n];

            if (m_tBuffer[n] > fZMax)
                fZMax = m_tBuffer[n];
        }
                
        // Spacing between two adjacent points in the buffer
        float fXSpacing = fXLength / (float)(m_nXSize - 1);
        float fYSpacing = fYLength / (float)(m_nYSize - 1);

        // Spacing used to convert the Z values from [ZMin, ZMax] to [0, 100]
        float fZSpacing = 100.0f / (fZMax - fZMin);

        // Iterates through the grid creating one subMesh at a time
        // We increment using GridSize-1 because we want that the last 
        // line & column of each grid starts the next one to avoid 
        for(int nX = 0; nX < m_nXSize; nX += (nGridXSize-1))
        {
            for(int nY = 0; nY < m_nYSize; nY += (nGridYSize-1))
            {
                // We need to re-calculate the size of the subMeshes at the border
                int nCurrentGridXSize = (nX + nGridXSize > m_nXSize) ? (m_nXSize - 1 - nX) : nGridXSize;
                int nCurrentGridYSize = (nY + nGridYSize > m_nYSize) ? (m_nYSize - 1 - nY) : nGridYSize;

                // Build One SubMesh
                BuildGameObjectAtFromBuffer(nX, nY, nCurrentGridXSize, nCurrentGridYSize,
                                            fXSpacing, fYSpacing, fZSpacing,
                                            fXOffset, fYOffset, fZMin);
            }
        }
    }


    //
    //  BuildGameObjectAtFromBuffer
    //
    //      Build a single subMesh from the Buffer
    //      It starts at (nStartX,nStartY) and is of Size (nGridXSize x nGridYSize)
    //      
    //      Spacing and Offset are used to calculate the vertices positions as follow:
    //      Position = Offset + Value * Spacing
    //
    private void BuildGameObjectAtFromBuffer(int nStartX, int nStartY, int nGridXSize, int nGridYSize,
                                             float fXSpacing, float fYSpacing, float fZSpacing,
                                             float fXOffset, float fYOffset, float fZOffset)
    {
        // Verification
        // We need at least 2x2 points to create a mesh
        if (nGridXSize < 2 || nGridYSize < 2)
            return;

        if (nStartX + nGridXSize > m_nXSize)
            return;

        if (nStartY + nGridYSize > m_nYSize)
            return;

        // Allocates all the vertices
        int nNumberOfVertices = nGridXSize * nGridYSize;
        Vector3[] tVertexBuffer = new Vector3[nNumberOfVertices];

        // Allocates all the indexes
        int nNumberOfIndexes = (nGridXSize - 1) * (nGridYSize - 1) * 6;
        int[] tIndexes = new int[nNumberOfIndexes];

        // Allocates the texture coordinates for a 1D palette..
        Vector2[] tTextureU = new Vector2[nNumberOfVertices];

        // Allocates the texture UVs for a 2D Texture
        Vector2[] tTextureUV = new Vector2[nNumberOfVertices];

        // Now let's fill all these buffer
        int nCurrentIndex = 0;
        for (int nX = 0; nX < nGridXSize; nX++)
        {
            for (int nY = 0; nY < nGridYSize; nY++)
            {
                // Current vertex index
                int nCurrentVertice = nX + nY * (nGridXSize);

                // Position
                tVertexBuffer[nCurrentVertice].x = fXOffset + (nStartX + nX) * fXSpacing;       // X to [-50, 50]
                tVertexBuffer[nCurrentVertice].y = fYOffset + (nStartY + nY) * fYSpacing;       // Y to [-50, 50]
                float fZValue = m_tBuffer[(nStartX + nX) + (nStartY + nY) * m_nXSize];
                tVertexBuffer[nCurrentVertice].z = (fZValue - fZOffset) * fZSpacing;            // Z to [0, 100]

                // Texture 1D
                // Used for palette type Textures (Vertical)
                tTextureU[nCurrentVertice].x = 0.5f;
                tTextureU[nCurrentVertice].y = tVertexBuffer[nCurrentVertice].z / 100.0f;   // Z to [0.0, 1.0]

                // Texture 2D
                tTextureUV[nCurrentVertice].x = (nStartX + nX) / (float)(m_nXSize - 1);     // U = X to [0.0, 1.0]
                tTextureUV[nCurrentVertice].y = (nStartY + nY) / (float)(m_nYSize - 1);     // V = Y to [0.0, 1.0]

                // Indexes
                // Skip the first line and column because we need at least a Quad to start declaring indexes
                if (nX == 0 || nY == 0)
                    continue;

                if(!m_bBetterMeshSplitting)
                {
                    // Adds the 6 Indexes of the quad
                    //      D - C
                    //      | \ |
                    //      B - A          
                    tIndexes[nCurrentIndex++] = nCurrentVertice - 1 - nGridXSize;   // D Top-Left
                    tIndexes[nCurrentIndex++] = nCurrentVertice - nGridXSize;       // C Top-Right
                    tIndexes[nCurrentIndex++] = nCurrentVertice;                    // A Bottom-Right

                    tIndexes[nCurrentIndex++] = nCurrentVertice;                    // A Bottom-Right
                    tIndexes[nCurrentIndex++] = nCurrentVertice - 1;                // B Bottom-Left
                    tIndexes[nCurrentIndex++] = nCurrentVertice - 1 - nGridXSize;   // D Top-Left
                }
                else
                {
                    // We'll split the quad by using the diagonal with the smallest height difference
                    //  if AD < BC                   if BC < AD           
                    //      D - C                       D - C   
                    //      | \ |                       | / |
                    //      B - A                       B - A
                    float fA = tVertexBuffer[nCurrentVertice].z;
                    float fB = tVertexBuffer[nCurrentVertice-1].z;
                    float fC = tVertexBuffer[nCurrentVertice - nGridXSize].z;
                    float fD = tVertexBuffer[nCurrentVertice - 1 - nGridXSize].z;

                    if(Mathf.Abs(fA - fD) < Mathf.Abs(fB - fC))
                    {
                        // Split with AD Diagonal DCA ABD
                        tIndexes[nCurrentIndex++] = nCurrentVertice - 1 - nGridXSize;   // D Top-Left
                        tIndexes[nCurrentIndex++] = nCurrentVertice - nGridXSize;       // C Top-Right
                        tIndexes[nCurrentIndex++] = nCurrentVertice;                    // A Bottom-Right

                        tIndexes[nCurrentIndex++] = nCurrentVertice;                    // A Bottom-Right
                        tIndexes[nCurrentIndex++] = nCurrentVertice - 1;                // B Bottom-Left
                        tIndexes[nCurrentIndex++] = nCurrentVertice - 1 - nGridXSize;   // D top-Lef
                    }
                    else
                    {
                        // Split with BC Diagonal DCB BCA
                        tIndexes[nCurrentIndex++] = nCurrentVertice - 1 - nGridXSize;   // D Top-Left
                        tIndexes[nCurrentIndex++] = nCurrentVertice - nGridXSize;       // C Top-Right
                        tIndexes[nCurrentIndex++] = nCurrentVertice - 1;                // B Bottom-Left

                        tIndexes[nCurrentIndex++] = nCurrentVertice - 1;                // B Bottom-Left
                        tIndexes[nCurrentIndex++] = nCurrentVertice - nGridXSize;       // C Top-Right
                        tIndexes[nCurrentIndex++] = nCurrentVertice;                    // A Bottom-Right
                    }
                }
            }
        }

        // Now that all the buffers are filled, we have to create a GO for them
        string sGOName = "SubMesh_X" + nStartX.ToString() + "_Y" + nStartY.ToString();
        GameObject goPlane = new GameObject(sGOName);

        // The GameObject will need a MeshFilter and a MeshRenderer
        goPlane.AddComponent<MeshFilter>();
        goPlane.AddComponent<MeshRenderer>();

        // Creation of the MeshObject
        Mesh currentMesh = new Mesh();
        currentMesh.vertices = tVertexBuffer;
        currentMesh.uv = tTextureU;
        currentMesh.uv2 = tTextureUV;
        currentMesh.triangles = tIndexes;
        
        // Calculates the mesh's normals
        currentMesh.RecalculateNormals();

        // Assign the mesh to the GO's meshFilter component
        goPlane.GetComponent<MeshFilter>().mesh = currentMesh;

        // Assign the default material to the plane
        goPlane.GetComponent<MeshRenderer>().material = m_DefaultMaterial;

        // Attach the GO to it's parent
        if (m_ParentTransform != null)
        {
            goPlane.transform.parent = m_ParentTransform;
            goPlane.transform.rotation = m_ParentTransform.rotation;
            goPlane.transform.localScale = m_ParentTransform.localScale;
        }            

        // Adds the GO to the list so we can destroy it easily after
        m_tGameObjects.Add(goPlane);
    }

    //
    // FracRandNum
    //
    //  Returns a RandomNumber between (-fCoeff, fCoeff)
    //
    private float fracRandNum(float fCoeff)
    {
        return (float)((m_RNG.NextDouble() * 2.0) - 1.0) * fCoeff;
    }

    // 
    // GetAt
    //  
    //  Returns the BufferValue at (nX, nY)
    //  
    private float GetAt(int nX, int nY)
    {
        int nIndex = nX + nY * m_nXSize;
        //if ((nIndex < 0) || (nIndex >= m_tBuffer.Length))
        if ((nIndex < 0) || (nIndex >= (m_nXSize * m_nYSize)))
        {
            // Houston, we have a problem
            return 0.0f;
        }            

        return m_tBuffer[nIndex];
    }

    // 
    // SetAt
    //  
    //  Modifies the BufferValue at (nX, nY)
    //  
    private void SetAt(int nX, int nY, float fValue)
    {
        int nIndex = nX + nY * m_nXSize;
        //if ((nIndex < 0) || (nIndex >= m_tBuffer.Length))
        if ((nIndex < 0) || (nIndex >= (m_nXSize * m_nYSize)))
            return;

        m_tBuffer[nIndex] = fValue;
    }

    //
    // IsOutOfBound
    //
    //  Return true if (nX, nY) is out of the buffer
    // 
    private bool IsOutOfBound(int nX, int nY)
    {
        if (nX < 0 || nX >= m_nXSize || nY < 0 || nY >= m_nYSize)
            return true;

        return false;
    }

    //
    // DoDiamondSquareStep
    //
    //  Performs one step of the Diamond-Square algorithm
    //  The size must be reduced (divided by 2) at each call, until nCurrentSize = 1
    // 
    private void DoDiamondSquareStep(int nCurrentSize)
    {
        // Verification
        if(nCurrentSize <= 1)
        {
            // nHalfSize will be 0 and we'll be stuck in a infinite loop
            return;
        }

        int nHalfSize = nCurrentSize / 2;

        // Fractal coefficient used to generate the random values
        // Reduces the range of the random values as nCurrentSize get smaller
        float fCoeff = Mathf.Pow(nHalfSize, (1 - m_fFractalDim));

        //
        //  Existing data 
        // 
        //  x . x	    x: Existing data
        //  . . .		.: Values to calculate				
        //  x . x
        //
        //  (here nCurrentSize = 2 and nHalfSize = 1)
        //

        // Iterates through all the square center and calculates their SquareMean
        for (int nY = nHalfSize; nY < m_nYSize; nY += nCurrentSize)
        {
            for (int nX = nHalfSize; nX < m_nXSize; nX += nCurrentSize)
            {
                //
                // Calculates the Mean+Random value of a square's center point
                // x . x	
                // . @ .	x: existing square data						
                // x . x    @: new point
                //
                SquareMean(nX, nY, nHalfSize, fCoeff);
            }
        }

        //
        // x . x
        // . x .
        // x . x
        //

        // Iterates through the remaining points and calculate their DiamondMean
        // The starting X position needs to be offset by nHalfSize once every two step
        bool bXOffset = true;
        for (int nY = 0; nY <= m_nYSize; nY += nHalfSize)
        {
            for (int nX = (bXOffset ? nHalfSize : 0); nX <= m_nXSize; nX += nCurrentSize)
            {
                //
                // Calculates the Mean+Random value of a diamond's center point
                // . x .	
                // x @ x	x: existing diamond data						
                // . x .    @: new point
                //
                DiamondMean(nX, nY, nHalfSize, fCoeff);
            }

            bXOffset = !bXOffset;
        }

        //
        // x x x
        // x x x
        // x x x
        //
    }


    //
    //  SquareMean
    //
    //      Calculates the Mean+Random value of a square's center point
    //      nHalfSize is the number of points from the center point to the border
    //      fCoeff is used to modify the range of the RandomValue
    //
    //      x: existing square data                         x . x	
    //      @: new point                                    . @ .							
    //                                                      x . x    
    //
    private void SquareMean(int nX, int nY, int nHalfSize, float fCoeff)
    {
        // If nX/nY is on the border, we need to take into account
        // that some values will be missing
        float fMean = 0.0f;
        float fDiv = 0.0f;

        // NorthWest point
        if (!IsOutOfBound(nX - nHalfSize, nY - nHalfSize))
        {
            fMean += GetAt(nX - nHalfSize, nY - nHalfSize);
            fDiv++;
        }

        // NorthEast point
        if (!IsOutOfBound(nX + nHalfSize, nY - nHalfSize))
        {
            fMean += GetAt(nX + nHalfSize, nY - nHalfSize);
            fDiv++;
        }

        // SouthEast point
        if (!IsOutOfBound(nX + nHalfSize, nY + nHalfSize))
        {
            fMean += GetAt(nX + nHalfSize, nY + nHalfSize);
            fDiv++;
        }

        // SouthWest point
        if (!IsOutOfBound(nX - nHalfSize, nY + nHalfSize))
        {
            fMean += GetAt(nX + nHalfSize, nY + nHalfSize);
            fDiv++;
        }

        if (fDiv > 0)
            fMean /= fDiv;

        SetAt(nX, nY, fMean + fracRandNum(fCoeff));
    }

    //
    //  DiamondMean
    //
    //      Calculates the Mean+Random value of a diamond's center point
    //      nHalfSize is the number of points from the center point to the border
    //      fCoeff is used to modify the range of the random value
    //
    //      x: existing diamond data                        . x .	
    //      @: new point                                    x @ x							
    //                                                      . x .    
    //
    private void DiamondMean(int x, int y, int size, float fCoeff)
    {
        // If nX/nY is on the border, we need to take into account
        // that some values will be missing
        float fMean = 0.0f;
        float fDiv = 0.0f;

        // Left point
        if (!IsOutOfBound(x - size, y))
        {
            fMean += GetAt(x - size, y);
            fDiv++;
        }

        // Right point
        if (!IsOutOfBound(x + size, y))
        {
            fMean += GetAt(x + size, y);
            fDiv++;
        }

        // Top point
        if (!IsOutOfBound(x, y - size))
        {
            fMean += GetAt(x, y - size);
            fDiv++;
        }

        // Bottom point
        if (!IsOutOfBound(x, y + size))
        {
            fMean += GetAt(x, y + size);
            fDiv++;
        }

        if (fDiv > 0)
            fMean /= fDiv;

        SetAt(x, y, fMean + fracRandNum(fCoeff));
    }

    //
    // SetComplexity
    //  
    //      Public function that modifies the complexity and ask for a mesh recreation
    //      (called by UI Slider)
    //      
    public void SetComplexity(float fValue)
    {
        if ((fValue < 1) || (fValue > 11))
            return;

        m_nComplexity = (int)fValue;
        m_bMeshNeedsRecreate = true;

        SetTimer();
    }

    //
    // SetFractalDimension
    //  
    //      Public function that modifies the fractal dimension and ask for a mesh recreation
    //      (called by UI Slider)
    //   
    public void SetFractalDimension(float fValue)
    {
        if ((fValue < 0.0f) || (fValue > 1.0f))
            return;

        m_fFractalDim = fValue;
        m_bMeshNeedsRecreate = true;

        SetTimer();
    }


    //
    // SetTimer
    //  
    //      Set a timer to avoid abusive call to generate by the UI
    //      The timer is longer when the complexity is high
    //  
    private void SetTimer()
    {
        // No Need to set a need Timer if it's already activated
        if (m_fMeshTimer > 0.0f)
            return;

        // Sets a simple timer for the mesh creation
        // For High complexity, the timer is longer
        if (m_nComplexity <= 6)
            m_fMeshTimer = (1.0f / 60.0f);
        else if (m_nComplexity <= 7)
            m_fMeshTimer = (1.0f / 50.0f);
        else if (m_nComplexity <= 8)
            m_fMeshTimer = (1.0f / 40.0f);
        else if (m_nComplexity <= 9)
            m_fMeshTimer = (1.0f / 10.0f);
        else
            m_fMeshTimer = 0.25f;
    }
}



