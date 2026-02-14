using Antymology.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Antymology.Terrain
{
    public class WorldManager : Singleton<WorldManager>
    {

        #region Fields

        /// <summary>
        /// The prefab containing the ant.
        /// </summary>
        public Ant antPrefab;

        public Queen queenPrefab;

        /// <summary>
        /// The material used for eech block.
        /// </summary>
        public Material blockMaterial;

        // how often the simulation should update
        public readonly float timeBetweenTicks = 0.25f;

        public float TimeSinceLastUpdate;

        // Number of ticks for this generation
        public int ElapsedTicks;

        // Which generation we're on
        public int Generation;

        // The number of nest blocks currently in the world for this generation
        public int NumNestBlocks;

        public readonly int TotalTicksPerGeneration = 100;

        public AbstractAnt[] Ants;

        /// <summary>
        /// The raw data of the underlying world structure.
        /// </summary>
        private AbstractBlock[,,] Blocks;

        public int NumAnts { get; private set; } = 500;

        /// <summary>
        /// Reference to the geometry data of the chunks.
        /// </summary>
        private Chunk[,,] Chunks;

        /// <summary>
        /// Random number generator.
        /// </summary>
        private System.Random RNG;

        /// <summary>
        /// Random number generator.
        /// </summary>
        private SimplexNoise SimplexNoise;

        // public HashSet<AirBlock> BlocksWithPheromone;

        #endregion

        #region Initialization

        /// <summary>
        /// Awake is called before any start method is called.
        /// </summary>
        void Awake()
        {

            ElapsedTicks = 0;

            Generation = 1;

            NumNestBlocks = 0;

            InitWorld();
        }

        // Initialize/reset all of the data fields for the world
        private void InitWorld()
        {
            // Generate new random number generator
            RNG = new System.Random(ConfigurationManager.Instance.Seed);

            // Generate new simplex noise generator
            SimplexNoise = new SimplexNoise(ConfigurationManager.Instance.Seed);

            // Initialize a new 3D array of blocks with size of the number of chunks times the size of each chunk
            Blocks = new AbstractBlock[
                ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter, //x
                ConfigurationManager.Instance.World_Height * ConfigurationManager.Instance.Chunk_Diameter, // y
                ConfigurationManager.Instance.World_Diameter * ConfigurationManager.Instance.Chunk_Diameter]; // z

            // Create list storing all ants and the queen
            Ants = new Ant[NumAnts+1];

            // Initialize a new 3D array of chunks with size of the number of chunks
            Chunks = new Chunk[
                ConfigurationManager.Instance.World_Diameter,
                ConfigurationManager.Instance.World_Height,
                ConfigurationManager.Instance.World_Diameter];         
        }

        /// <summary>
        /// Called after every awake has been called.
        /// </summary>
        private void Start()
        {
            GenerateData();
            GenerateChunks();

            Camera.main.transform.position = new Vector3(0 / 2, Blocks.GetLength(1), 0);
            Camera.main.transform.LookAt(new Vector3(Blocks.GetLength(0), 0, Blocks.GetLength(2)));

            // When we first start the simultation, weights ants use in their decision models are randomized
            List<List<double[,]>> ModelWeights = GenerateRandomWeights();
            GenerateAnts(ModelWeights);
        }

        private List<List<double[,]>> GenerateRandomWeights()
        {
            List<List<double[,]>> AllWeights = new();
            for (int i = 0; i < NumAnts; i++)
            {
                List<double[,]> weightsForModel = new();

                double[,] FirstLayerWeights = new double[16,10];
                for (int j = 0; j < FirstLayerWeights.GetLength(0); j++)
                {
                    for (int k = 0; k < FirstLayerWeights.GetLength(1); k++)
                    {
                        // Generate random weight between -50 and 50
                        FirstLayerWeights[j,k] = RNG.NextDouble();
                    }
                }

                weightsForModel.Add(FirstLayerWeights);
                
                double[,] secondlayerweights = new double[5,16];
                for (int j = 0; j < secondlayerweights.GetLength(0); j++)
                {
                    for (int k = 0; k < secondlayerweights.GetLength(1); k++)
                    {
                        secondlayerweights[j,k] = RNG.NextDouble();
                    }
                }

                weightsForModel.Add(secondlayerweights);

                AllWeights.Add(weightsForModel);
            }

            return AllWeights;
        }
        // Generate ants to spawn on top of a random mulch block.
        private void GenerateAnts(List<List<double[,]>> ModelWeights)
        {
            List<int[]> MulchBlocks = GetSurfaceMulchBlocks();

            // Do this just in case for some reason the number of ants we choose to generate is larger than
            // the possible spawn locations
            // int NumToGenerate = Math.Min(NumAnts, MulchBlocks.Count);

            // create an iterable of integers between 0 and the number of grass blocks
            // to represent the indices of the GrassBlock list
            List<int> Ids = Enumerable.Range(0, MulchBlocks.Count)
                            .OrderBy(item => RNG.Next()).Take(NumAnts).ToList();
                            // Use a series of random numbers to shuffle the indices, then take the first n
                            // items of this list. This should give us spawn locations for the n ants we want to generate

            for (int i = 0; i < NumAnts; i++)
            {
                // Block coordinates where ant should spawn
                int XSpawn, YSpawn, ZSpawn;

                // Get coordinates of ith randomly selected mulch block
                int[] MulchCoord = MulchBlocks[Ids[i]];

                // Set spawn coordinates to coordinates of block
                XSpawn = MulchCoord[0];
                YSpawn = MulchCoord[1];
                ZSpawn = MulchCoord[2];

                // Create instance of Ant and move it to be on top of mulch block
                Ant NewAnt = Instantiate<Ant>(antPrefab, transform, false);
                NewAnt.block_x = XSpawn;
                NewAnt.block_y = YSpawn;
                NewAnt.block_z = ZSpawn;

                NewAnt.id = i;

                // Need to slightly adjust the Y positioning so that ant is standing on top of correct block
                NewAnt.transform.position = new Vector3(XSpawn, YSpawn-1.2f, ZSpawn);
                
                
                // Create ant's decision model using weights provided
                DecisionModel AntModel = new(10, 4);
                
                List<double[,]> weights = ModelWeights[i];
                double[,] firstLayerWeights = weights[0];
                double[,] secondLayerWeights = weights[1];

                AntModel.AddFirstLayer(16, Activations.ReLU, firstLayerWeights);
                AntModel.AddLayer(4, Activations.Linear, secondLayerWeights);

                NewAnt.Model = AntModel;
                Ants[i] = NewAnt;
            }

            int QueenX, QueenY, QueenZ;

            Queen NewQueen = Instantiate<Queen>(queenPrefab, transform, false);

            // Spawn queen at about the center of the map
            QueenX = Mathf.FloorToInt(Blocks.GetLength(0) / 2);
            QueenZ = Mathf.FloorToInt(Blocks.GetLength(2) / 2);

            QueenY = FindFirstSolidBlock(QueenX, QueenZ);

            NewQueen.block_x = QueenX;
            NewQueen.block_y = QueenY;
            NewQueen.block_z = QueenZ;

            NewQueen.transform.position = new Vector3(QueenX, QueenY-1.2f, QueenZ);

            NewQueen.id = NumAnts;

            Ants[NumAnts] = NewQueen;
        }

        #endregion

        #region Methods

        // At given x and z coordinates, find the y coordinate of the first block that is below an air block
        public int FindFirstSolidBlock(int x, int z)
        {
            int y;
            for (y = Blocks.GetLength(1)-2; y >= 0; y--)
            {
                AbstractBlock b = Blocks[x,y,z];
                if (b is not AirBlock)
                {
                    return y;
                }
            }
            return y;
        }

        // finds all of the mulch blocks that have an air block directly above it
        // Used to find possible spawn locations of the ants when world is generated
        private List<int[]> GetSurfaceMulchBlocks()
        {
            List<int[]> MulchBlocks = new List<int[]>();
            int j;
            for (int i = 0; i < Blocks.GetLength(0); i++)
            {
                for (int k = 0; k < Blocks.GetLength(2); k++)
                {
                    j = FindFirstSolidBlock(i, k);
                    if (Blocks[i,j,k] is MulchBlock)
                    {
                        MulchBlocks.Add(new int[]{i,j,k});
                    }
                }
            }
            return MulchBlocks;
        }

        // Get a list of other ants that are on the same block as an ant with id CallerID
        public List<AbstractAnt> OtherAntsAt(int CallerID, int WorldXCoordinate, int WorldYCoordinate, int WorldZCoordinate)
        {
            List<AbstractAnt> Result = new();
            for (int i = 0; i < Ants.Length; i++)
            {
                AbstractAnt a = Ants[i];
                if ((a.id != CallerID) && a.block_x == WorldXCoordinate && a.block_y == WorldYCoordinate && a.block_z == WorldZCoordinate)
                {
                    Result.Add(a);
                }
            }

            return Result;
        }
        /// <summary>
        /// Retrieves an abstract block type at the desired world coordinates.
        /// </summary>
        public AbstractBlock GetBlock(int WorldXCoordinate, int WorldYCoordinate, int WorldZCoordinate)
        {
            if
            (
                WorldXCoordinate < 0 ||
                WorldYCoordinate < 0 ||
                WorldZCoordinate < 0 ||
                WorldXCoordinate >= Blocks.GetLength(0) ||
                WorldYCoordinate >= Blocks.GetLength(1) ||
                WorldZCoordinate >= Blocks.GetLength(2)
            )
                return new AirBlock();

            return Blocks[WorldXCoordinate, WorldYCoordinate, WorldZCoordinate];
        }

        /// <summary>
        /// Retrieves an abstract block type at the desired local coordinates within a chunk.
        /// </summary>
        public AbstractBlock GetBlock(
            int ChunkXCoordinate, int ChunkYCoordinate, int ChunkZCoordinate,
            int LocalXCoordinate, int LocalYCoordinate, int LocalZCoordinate)
        {
            if
            (
                LocalXCoordinate < 0 ||
                LocalYCoordinate < 0 ||
                LocalZCoordinate < 0 ||
                LocalXCoordinate >= Blocks.GetLength(0) ||
                LocalYCoordinate >= Blocks.GetLength(1) ||
                LocalZCoordinate >= Blocks.GetLength(2) ||
                ChunkXCoordinate < 0 ||
                ChunkYCoordinate < 0 ||
                ChunkZCoordinate < 0 ||
                ChunkXCoordinate >= Blocks.GetLength(0) ||
                ChunkYCoordinate >= Blocks.GetLength(1) ||
                ChunkZCoordinate >= Blocks.GetLength(2) 
            )
                return new AirBlock();

            return Blocks
            [
                ChunkXCoordinate * LocalXCoordinate,
                ChunkYCoordinate * LocalYCoordinate,
                ChunkZCoordinate * LocalZCoordinate
            ];
        }

        /// <summary>
        /// sets an abstract block type at the desired world coordinates.
        /// </summary>
        public void SetBlock(int WorldXCoordinate, int WorldYCoordinate, int WorldZCoordinate, AbstractBlock toSet)
        {
            if
            (
                WorldXCoordinate < 0 ||
                WorldYCoordinate < 0 ||
                WorldZCoordinate < 0 ||
                WorldXCoordinate >= Blocks.GetLength(0) ||
                WorldYCoordinate >= Blocks.GetLength(1) ||
                WorldZCoordinate >= Blocks.GetLength(2)
            )
            {
                Debug.Log("Attempted to set a block which didn't exist");
                return;
            }

            Blocks[WorldXCoordinate, WorldYCoordinate, WorldZCoordinate] = toSet;

            SetChunkContainingBlockToUpdate
            (
                WorldXCoordinate,
                WorldYCoordinate,
                WorldZCoordinate
            );
        }

        /// <summary>
        /// sets an abstract block type at the desired local coordinates within a chunk.
        /// </summary>
        public void SetBlock(
            int ChunkXCoordinate, int ChunkYCoordinate, int ChunkZCoordinate,
            int LocalXCoordinate, int LocalYCoordinate, int LocalZCoordinate,
            AbstractBlock toSet)
        {
            if
            (
                LocalXCoordinate < 0 ||
                LocalYCoordinate < 0 ||
                LocalZCoordinate < 0 ||
                LocalXCoordinate > Blocks.GetLength(0) ||
                LocalYCoordinate > Blocks.GetLength(1) ||
                LocalZCoordinate > Blocks.GetLength(2) ||
                ChunkXCoordinate < 0 ||
                ChunkYCoordinate < 0 ||
                ChunkZCoordinate < 0 ||
                ChunkXCoordinate > Blocks.GetLength(0) ||
                ChunkYCoordinate > Blocks.GetLength(1) ||
                ChunkZCoordinate > Blocks.GetLength(2)
            )
            {
                Debug.Log("Attempted to set a block which didn't exist");
                return;
            }
            Blocks
            [
                ChunkXCoordinate * LocalXCoordinate,
                ChunkYCoordinate * LocalYCoordinate,
                ChunkZCoordinate * LocalZCoordinate
            ] = toSet;

            SetChunkContainingBlockToUpdate
            (
                ChunkXCoordinate * LocalXCoordinate,
                ChunkYCoordinate * LocalYCoordinate,
                ChunkZCoordinate * LocalZCoordinate
            );
        }

        #endregion

        #region Helpers

        #region Blocks

        /// <summary>
        /// Is responsible for generating the base, acid, and spheres.
        /// </summary>
        private void GenerateData()
        {
            GeneratePreliminaryWorld();
            GenerateAcidicRegions();
            GenerateSphericalContainers();
        }

        /// <summary>
        /// Generates the preliminary world data based on perlin noise.
        /// </summary>
        private void GeneratePreliminaryWorld()
        {
            for (int x = 0; x < Blocks.GetLength(0); x++)
                for (int z = 0; z < Blocks.GetLength(2); z++)
                {
                    /**
                     * These numbers have been fine-tuned and tweaked through trial and error.
                     * Altering these numbers may produce weird looking worlds.
                     **/
                    int stoneCeiling = SimplexNoise.GetPerlinNoise(x, 0, z, 10, 3, 1.2) +
                                       SimplexNoise.GetPerlinNoise(x, 300, z, 20, 4, 0) +
                                       10;
                    int grassHeight = SimplexNoise.GetPerlinNoise(x, 100, z, 30, 10, 0);
                    int foodHeight = SimplexNoise.GetPerlinNoise(x, 200, z, 20, 5, 1.5);

                    for (int y = 0; y < Blocks.GetLength(1); y++)
                    {
                        if (y <= stoneCeiling)
                        {
                            Blocks[x, y, z] = new StoneBlock();
                        }
                        else if (y <= stoneCeiling + grassHeight)
                        {
                            Blocks[x, y, z] = new GrassBlock();
                        }
                        else if (y <= stoneCeiling + grassHeight + foodHeight)
                        {
                            Blocks[x, y, z] = new MulchBlock();
                        }
                        else
                        {
                            Blocks[x, y, z] = new AirBlock(x, y, z);
                        }
                        if
                        (
                            x == 0 ||
                            x >= Blocks.GetLength(0) - 1 ||
                            z == 0 ||
                            z >= Blocks.GetLength(2) - 1 ||
                            y == 0
                        )
                            Blocks[x, y, z] = new ContainerBlock();
                    }
                }
        }

        /// <summary>
        /// Alters a pre-generated map so that acid blocks exist.
        /// </summary>
        private void GenerateAcidicRegions()
        {
            for (int i = 0; i < ConfigurationManager.Instance.Number_Of_Acidic_Regions; i++)
            {
                int xCoord = RNG.Next(0, Blocks.GetLength(0));
                int zCoord = RNG.Next(0, Blocks.GetLength(2));
                int yCoord = -1;
                for (int j = Blocks.GetLength(1) - 1; j >= 0; j--)
                {
                    if (Blocks[xCoord, j, zCoord] as AirBlock == null)
                    {
                        yCoord = j;
                        break;
                    }
                }

                //Generate a sphere around this point overriding non-air blocks
                for (int HX = xCoord - ConfigurationManager.Instance.Acidic_Region_Radius; HX < xCoord + ConfigurationManager.Instance.Acidic_Region_Radius; HX++)
                {
                    for (int HZ = zCoord - ConfigurationManager.Instance.Acidic_Region_Radius; HZ < zCoord + ConfigurationManager.Instance.Acidic_Region_Radius; HZ++)
                    {
                        for (int HY = yCoord - ConfigurationManager.Instance.Acidic_Region_Radius; HY < yCoord + ConfigurationManager.Instance.Acidic_Region_Radius; HY++)
                        {
                            float xSquare = (xCoord - HX) * (xCoord - HX);
                            float ySquare = (yCoord - HY) * (yCoord - HY);
                            float zSquare = (zCoord - HZ) * (zCoord - HZ);
                            float Dist = Mathf.Sqrt(xSquare + ySquare + zSquare);
                            if (Dist <= ConfigurationManager.Instance.Acidic_Region_Radius)
                            {
                                int CX, CY, CZ;
                                CX = Mathf.Clamp(HX, 1, Blocks.GetLength(0) - 2);
                                CZ = Mathf.Clamp(HZ, 1, Blocks.GetLength(2) - 2);
                                CY = Mathf.Clamp(HY, 1, Blocks.GetLength(1) - 2);
                                if (Blocks[CX, CY, CZ] as AirBlock != null)
                                    Blocks[CX, CY, CZ] = new AcidicBlock();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Alters a pre-generated map so that obstructions exist within the map.
        /// </summary>
        private void GenerateSphericalContainers()
        {

            //Generate hazards
            for (int i = 0; i < ConfigurationManager.Instance.Number_Of_Conatiner_Spheres; i++)
            {
                int xCoord = RNG.Next(0, Blocks.GetLength(0));
                int zCoord = RNG.Next(0, Blocks.GetLength(2));
                int yCoord = RNG.Next(0, Blocks.GetLength(1));


                //Generate a sphere around this point overriding non-air blocks
                for (int HX = xCoord - ConfigurationManager.Instance.Conatiner_Sphere_Radius; HX < xCoord + ConfigurationManager.Instance.Conatiner_Sphere_Radius; HX++)
                {
                    for (int HZ = zCoord - ConfigurationManager.Instance.Conatiner_Sphere_Radius; HZ < zCoord + ConfigurationManager.Instance.Conatiner_Sphere_Radius; HZ++)
                    {
                        for (int HY = yCoord - ConfigurationManager.Instance.Conatiner_Sphere_Radius; HY < yCoord + ConfigurationManager.Instance.Conatiner_Sphere_Radius; HY++)
                        {
                            float xSquare = (xCoord - HX) * (xCoord - HX);
                            float ySquare = (yCoord - HY) * (yCoord - HY);
                            float zSquare = (zCoord - HZ) * (zCoord - HZ);
                            float Dist = Mathf.Sqrt(xSquare + ySquare + zSquare);
                            if (Dist <= ConfigurationManager.Instance.Conatiner_Sphere_Radius)
                            {
                                int CX, CY, CZ;
                                CX = Mathf.Clamp(HX, 1, Blocks.GetLength(0) - 2);
                                CZ = Mathf.Clamp(HZ, 1, Blocks.GetLength(2) - 2);
                                CY = Mathf.Clamp(HY, 1, Blocks.GetLength(1) - 2);
                                Blocks[CX, CY, CZ] = new ContainerBlock();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Given a world coordinate, tells the chunk holding that coordinate to update.
        /// Also tells all 4 neighbours to update (as an altered block might exist on the
        /// edge of a chunk).
        /// </summary>
        /// <param name="worldXCoordinate"></param>
        /// <param name="worldYCoordinate"></param>
        /// <param name="worldZCoordinate"></param>
        private void SetChunkContainingBlockToUpdate(int worldXCoordinate, int worldYCoordinate, int worldZCoordinate)
        {
            //Updates the chunk containing this block
            int updateX = Mathf.FloorToInt(worldXCoordinate / ConfigurationManager.Instance.Chunk_Diameter);
            int updateY = Mathf.FloorToInt(worldYCoordinate / ConfigurationManager.Instance.Chunk_Diameter);
            int updateZ = Mathf.FloorToInt(worldZCoordinate / ConfigurationManager.Instance.Chunk_Diameter);
            Chunks[updateX, updateY, updateZ].updateNeeded = true;
            
            // Also flag all 6 neighbours for update as well
            if(updateX - 1 >= 0)
                Chunks[updateX - 1, updateY, updateZ].updateNeeded = true;
            if (updateX + 1 < Chunks.GetLength(0))
                Chunks[updateX + 1, updateY, updateZ].updateNeeded = true;

            if (updateY - 1 >= 0)
                Chunks[updateX, updateY - 1, updateZ].updateNeeded = true;
            if (updateY + 1 < Chunks.GetLength(1))
                Chunks[updateX, updateY + 1, updateZ].updateNeeded = true;

            if (updateZ - 1 >= 0)
                Chunks[updateX, updateY, updateZ - 1].updateNeeded = true;
            if (updateZ + 1 < Chunks.GetLength(2))
                Chunks[updateX, updateY, updateZ + 1].updateNeeded = true;
        }

        #endregion
        

        #region Chunks

        /// <summary>
        /// Takes the world data and generates the associated chunk objects.
        /// </summary>
        private void GenerateChunks()
        {
            GameObject chunkObg = new GameObject("Chunks");
            chunkObg.tag = "Chunk";

            for (int x = 0; x < Chunks.GetLength(0); x++)
                for (int z = 0; z < Chunks.GetLength(2); z++)
                    for (int y = 0; y < Chunks.GetLength(1); y++)
                    {
                        GameObject temp = new GameObject();
                        temp.transform.parent = chunkObg.transform;
                        temp.transform.position = new Vector3
                        (
                            x * ConfigurationManager.Instance.Chunk_Diameter - 0.5f,
                            y * ConfigurationManager.Instance.Chunk_Diameter + 0.5f,
                            z * ConfigurationManager.Instance.Chunk_Diameter - 0.5f
                        );
                        Chunk chunkScript = temp.AddComponent<Chunk>();
                        chunkScript.x = x * ConfigurationManager.Instance.Chunk_Diameter;
                        chunkScript.y = y * ConfigurationManager.Instance.Chunk_Diameter;
                        chunkScript.z = z * ConfigurationManager.Instance.Chunk_Diameter;
                        chunkScript.Init(blockMaterial);
                        chunkScript.GenerateMesh();
                        Chunks[x, y, z] = chunkScript;
                    }
        }

        #endregion

        #endregion

        #region Updates

        // Called after the ants do their updates so that air blocks have most
        // up to date pheromone concentrations, and if we're resetting, we do so after
        // the ants finished their last update
        void LateUpdate()
        {
            if (ElapsedTicks < TotalTicksPerGeneration)
            {
                TimeSinceLastUpdate += Time.deltaTime;
                if (TimeSinceLastUpdate >= timeBetweenTicks)
                {
                    TimeSinceLastUpdate -= timeBetweenTicks;
                    DiffusePheromones();
                    ElapsedTicks++;
                    UIManager.Instance.UpdateText(NumNestBlocks, Generation, ElapsedTicks);
                } 
            }
            else
            {
                ResetWorld();
            }
        }

        // Called when we need to make the next generation of ants
        // Destroys all blocks and rebuilds the world
        // Then breeds the ants and repopulates the world with the new generation of ants
        void ResetWorld()
        {
            GameObject[] Chunks = GameObject.FindGameObjectsWithTag("Chunk");
            for (int i = Chunks.GetLength(0)-1; i >= 0; i--)
            {
                Destroy(Chunks[i]);
            }

            foreach (AbstractAnt A in Ants)
            {
                A.gameObject.SetActive(true);
            }

            List<List<double[,]>> NextGenModelWeights = EvolutionaryStrategy.MakeNextGen(Ants);

            foreach (AbstractAnt A in Ants)
            {
                Destroy(A.gameObject);
            }

            ElapsedTicks = 0;
            Generation++;
            NumNestBlocks = 0;

            InitWorld();

            GenerateData();
            GenerateChunks();

            GenerateAnts(NextGenModelWeights);
        }

        // Air blocks with pheromones spread the pheromones to neighbouring air blocks
        void DiffusePheromones()
        {
            for (int x = 0; x < Blocks.GetLength(0); x++)
            {
                for (int y = 0; y < Blocks.GetLength(1); y++)
                {
                    for (int z = 0; z < Blocks.GetLength(2); z++)
                    {
                        AbstractBlock block = GetBlock(x, y, z);
                        if (block is AirBlock air)
                        {
                            if (air.GetPheromones() > 0)
                            {
                                List<AirBlock> neighbours = GetNeighbouringAirBlocks(x, y, z);
                                air.Diffuse(neighbours);
                                air.Evaporate();
                            }
                        }
                    }
                }
            }
        }

        // Find air blocks that are adjacent to block at the given coordinates
        List<AirBlock> GetNeighbouringAirBlocks(int x, int y, int z)
        {
            List<AirBlock> Result = new();
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    for (int k = -1; k <= 1; k++)
                    {
                        if ((i+j+k) != 0)
                        {
                            AbstractBlock Neighbour = GetBlock(x+i, y+j, z+k);
                            if (Neighbour is AirBlock block)
                            {
                                Result.Add(block);
                            }
                        }
                    }
                }
            }
            return Result;
        }

    #endregion
    }
}