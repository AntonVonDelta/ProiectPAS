using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static VerletIntegration;

public class Branch {
    public struct Attachment {
        public int nodeIndex;
        public Branch childBranch;
    }

    private List<Attachment> childBranches = new List<Attachment>();
    private List<RigidLine> rigidLines = new List<RigidLine>();
    private int order = 0;
    private Branch parent;
    private int startingNodeIndex = -1;
    private int nodeCount;
    private float distance;

    public Branch(Branch parent,int startingNodeIndex, int nodeCount, float distance = 1f) {
        this.parent = parent;
        this.startingNodeIndex = startingNodeIndex;
        this.nodeCount = nodeCount;
        this.distance = distance;

        ConnectPointsByLines(nodeCount, distance);

        if (parent != null) {
            order = parent.GetOrder() + 1;
        }
    }

    public Attachment AddChildBranch(int relativeAttachmentNodeIndex, Branch child) {
        Attachment newAttachment = new Attachment { nodeIndex = startingNodeIndex + relativeAttachmentNodeIndex, childBranch = child };
        childBranches.Add(newAttachment);
        return newAttachment;
    }

    public int GetOrder() {
        return order;
    }

    public Branch GetParent() {
        return parent;
    }
    public int GetStartingNodeIndex() {
        return startingNodeIndex;
    }

    public int GetNodeCount() {
        return nodeCount;
    }
    public float GetDistance() {
        return distance;
    }

    public List<Attachment> GetChildBranches() {
        return childBranches;
    }


    public List<RigidLine> GetRigidLines() {
        return rigidLines;
    }



    private void ConnectPointsByLines(int count, float distance) {
        for (int i = 0; i < count - 1; i++) {
            rigidLines.Add(new RigidLine(startingNodeIndex + i, startingNodeIndex + i + 1, distance));
        }
    }
}
