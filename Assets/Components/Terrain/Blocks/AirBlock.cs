using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Antymology.Terrain
{
    /// <summary>
    /// The air type of block. Contains the internal data representing phermones in the air.
    /// </summary>
    public class AirBlock : AbstractBlock
    {

        #region Fields

        /// <summary>
        /// Statically held is visible.
        /// </summary>
        private static bool _isVisible = false;

        private static double MaxConcentration = 100;

        private static double EvaporationRate = .9;

        private static double DiffusionRate = 0.02;

        /// <summary>
        /// A dictionary representing the phermone deposits in the air. Each type of phermone gets it's own byte key, and each phermone type has a concentration.
        /// THIS CURRENTLY ONLY EXISTS AS A WAY OF SHOWING YOU HOW YOU CAN MANIPULATE THE BLOCKS.
        /// </summary>
        public double phermoneDeposits;

        #endregion

        #region Methods

        /// <summary>
        /// Air blocks are going to be invisible.
        /// </summary>
        public override bool isVisible()
        {
            return _isVisible;
        }

        /// <summary>
        /// Air blocks are invisible so asking for their tile map coordinate doesn't make sense.
        /// </summary>
        public override Vector2 tileMapCoordinate()
        {
            throw new Exception("An invisible tile cannot have a tile map coordinate.");
        }

        public AirBlock(int x, int y, int z) : base(x, y, z)
        {
            phermoneDeposits = 0;
        }

        public AirBlock() : base()
        {
            phermoneDeposits = 0;
        }

        /// <summary>
        /// THIS CURRENTLY ONLY EXISTS AS A WAY OF SHOWING YOU WHATS POSSIBLE.
        /// </summary>
        /// <param name="neighbours"></param>
        public void Diffuse(List<AirBlock> neighbours)
        {
            // List<AirBlock> AirBlocks = neighbours.Where(B => B is AirBlock).Select(B => (AirBlock) B).ToList();
            if (neighbours.Count > 0) {
                double AmountLost = phermoneDeposits * DiffusionRate;
                double AmountGiven = AmountLost / neighbours.Count;
                foreach (AirBlock B in neighbours)
                {
                    B.AddPheromones(AmountGiven);
                }
                phermoneDeposits = Math.Max(0, phermoneDeposits - AmountLost);
            }

            // Debug.Log("Air Block Has Diffused Pheromones");
        }

        public void Evaporate()
        {
            phermoneDeposits *= EvaporationRate;
            if (phermoneDeposits <= 1)
            {
                phermoneDeposits = 0;
                // WorldManager.Instance.BlocksWithPheromone.Remove(this);
            }
        }

        public double GetPheromones()
        {
            return phermoneDeposits;
        }

        public void AddPheromones(double Amount)
        {
            if (phermoneDeposits + Amount > MaxConcentration)
            {
                phermoneDeposits = MaxConcentration;
            }
            else if (phermoneDeposits + Amount < 0)
            {
                phermoneDeposits = 0;
            }
            else
            {
                phermoneDeposits += Amount;
            }
            // Debug.Log($"Air Block at {worldXCoordinate}, {worldYCoordinate}, {worldZCoordinate} has {phermoneDeposits} units of pheromone");
        }
        #endregion

    }
}
