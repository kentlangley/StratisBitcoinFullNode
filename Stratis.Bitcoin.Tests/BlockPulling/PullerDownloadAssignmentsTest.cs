﻿using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Stratis.Bitcoin.BlockPulling;

namespace Stratis.Bitcoin.Tests.BlockPulling
{
    /// <summary>
    /// Tests of PullerDownloadAssignments class.
    /// </summary>
    public class PullerDownloadAssignmentsTest
    {
        /// <summary>
        /// Previous implementation of block puller's strategy could lead to a situation in which the node's 
        /// peers were asked for blocks then did not have. This is undesirable.
        /// <para>
        /// We simulate the following scenario in this test:
        /// <list type="bullet">
        /// <item>Our node has a chain with 5 blocks and is connected to 4 peer nodes - A, B, C, D.</item>
        /// <item>Node A has a chain with 4 blocks.</item>
        /// <item>Node B has a chain with 20 blocks.</item>
        /// <item>Node C has a chain with 30 blocks.</item>
        /// <item>Node D has a chain with 40 blocks.</item>
        /// </list>
        /// </para>
        /// <para>
        /// We call AskBlocks on the block puller with requests to download blocks 6 to 40
        /// and we check that the node A is not assigned any work and that the node B is not 
        /// assigned any work for blocks 21 to 40, and C is not assigned any work for blocks 
        /// 31 to 40.
        /// </para>
        /// </summary>
        [Fact]
        public void AssignBlocksToPeersWithNodesWithDifferentChainsCorrectlyDistributesDownloadTasks()
        {
            // Create list of numbers 6 to 40 and shuffle it.
            Random rnd = new Random();
            List<int> requiredBlockHeights = new List<int>();
            for (int i = 6; i <= 40; i++)
                requiredBlockHeights.Add(i);

            requiredBlockHeights = requiredBlockHeights.OrderBy(a => rnd.Next()).ToList();

            // Initialize node's peers.
            List<PullerDownloadAssignments.PeerInformation> availablePeersInformation = new List<PullerDownloadAssignments.PeerInformation>()
            {
                new PullerDownloadAssignments.PeerInformation()
                {
                    PeerId = "A",
                    QualityScore = 100,
                    ChainHeight = 4
                },
                new PullerDownloadAssignments.PeerInformation()
                {
                    PeerId = "B",
                    QualityScore = 100,
                    ChainHeight = 20
                },
                new PullerDownloadAssignments.PeerInformation()
                {
                    PeerId = "C",
                    QualityScore = 50,
                    ChainHeight = 30
                },
                new PullerDownloadAssignments.PeerInformation()
                {
                    PeerId = "C",
                    QualityScore = 150,
                    ChainHeight = 40
                },
            };

            // Use the assignment strategy to assign tasks to peers.
            Dictionary<PullerDownloadAssignments.PeerInformation, List<int>> assignments = PullerDownloadAssignments.AssignBlocksToPeers(requiredBlockHeights, availablePeersInformation);


            // Check the assignment is valid per our requirements.
            int tasksAssigned = 0;
            Assert.Equal(4, assignments.Count);
            foreach (KeyValuePair<PullerDownloadAssignments.PeerInformation, List<int>> kvp in assignments)
            {
                PullerDownloadAssignments.PeerInformation peer = kvp.Key;
                List<int> assignedBlockHeights = kvp.Value;
                tasksAssigned += assignedBlockHeights.Count;

                switch ((string)peer.PeerId)
                {
                    case "A":
                        // Peer A should not get any work.
                        Assert.Equal(0, assignedBlockHeights.Count);
                        break;

                    case "B":
                    case "C":
                    case "D":
                        // Peers B and C should only get tasks to download blocks up to its chain height.
                        // Peer D can be assigned anything.
                        Assert.True(assignedBlockHeights.Max() <= peer.ChainHeight);
                        break;

                    default:
                        // This should never occur.
                        Assert.True(false, "Invalid peer ID.");
                        break;
                }
            }
            Assert.Equal(requiredBlockHeights.Count, tasksAssigned);
        }
    }
}
