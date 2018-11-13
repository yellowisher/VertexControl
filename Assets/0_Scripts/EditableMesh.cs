using System.Collections.Generic;
using UnityEngine;

public class EditableMesh : MonoBehaviour
{
	private static List<EditableThing> things = new List<EditableThing>();

	public EditableVertex editableVertexPrefab;
	public EditableLine editableLinePrefab;

	private Mesh mesh;

	private bool updated = false;
	private List<Vector3> vertices = new List<Vector3>();

	private Dictionary<EditableVertex, List<IUpdatable>> listeningList = new Dictionary<EditableVertex, List<IUpdatable>>();

	private struct LineKey
	{
		public LineKey(EditableVertex p0, EditableVertex p1)
		{
			this.p0 = p0;
			this.p1 = p1;
		}

		public override bool Equals(object obj)
		{
			if (obj == null || !(obj is LineKey))
			{
				return false;
			}
			LineKey other = (LineKey)obj;
			return (other.p0 == p0 && other.p1 == p1) || (other.p0 == p1 && other.p1 == p0);
		}

		public override int GetHashCode()
		{
			return p0.GetHashCode() + p1.GetHashCode();
		}

		public EditableVertex p0;
		public EditableVertex p1;
	}

	private void Awake()
	{
		Quaternion rotation = transform.rotation;
		transform.rotation = Quaternion.identity;

		Init();

		transform.rotation = rotation;
	}

	public void Init()
	{
		mesh = GetComponentInChildren<MeshFilter>().mesh;

		Dictionary<Vector3, List<int>> vertexDict = new Dictionary<Vector3, List<int>>();

		Vector3[] vertices = mesh.vertices;
		for (int v = 0; v < vertices.Length; v++)
		{
			if (!vertexDict.ContainsKey(vertices[v])) vertexDict[vertices[v]] = new List<int>();
			vertexDict[vertices[v]].Add(v);
			this.vertices.Add(vertices[v]);
		}

		EditableVertex[] eVertices = new EditableVertex[mesh.vertexCount];

		foreach (var vertex in vertexDict.Keys)
		{
			Vector3 scaleVertex = vertex;
			scaleVertex.x *= transform.localScale.x;
			scaleVertex.y *= transform.localScale.y;
			scaleVertex.z *= transform.localScale.z;

			EditableVertex eVertex = Instantiate(editableVertexPrefab, scaleVertex + transform.position, Quaternion.identity);
			eVertex.Initialize(this, vertexDict[vertex]);
			foreach (var idx in vertexDict[vertex])
			{
				eVertices[idx] = eVertex;
			}
		}

		// {0, 1}, {0, 2}, {1, 2}
		int[] aArr = new int[] { 0, 0, 1 };
		int[] bArr = new int[] { 1, 2, 2 };

		var lines = new Dictionary<LineKey, EditableLine>();

		int[] triangles = mesh.triangles;
		for (int t = 0; t < triangles.Length; t += 3)
		{
			int idx = triangles[t];

			for (int i = 0; i < 3; i++)
			{
				int ta = t + aArr[i];
				int tb = t + bArr[i];

				LineKey key = new LineKey(eVertices[triangles[ta]], eVertices[triangles[tb]]);
				if (lines.ContainsKey(key))
				{
					lines[key].tIndices.Add(idx);
				}
				else
				{
					EditableLine line = Instantiate(editableLinePrefab);
					line.Initialize(this, eVertices[triangles[ta]], eVertices[triangles[tb]], idx);
					lines[key] = line;
				}
			}
		}

		var pairsToDelete = new List<KeyValuePair<LineKey, EditableLine>>();

		Vector3[] normals = mesh.normals;
		foreach (var linePair in lines)
		{
			bool delete = true;
			EditableLine line = linePair.Value;

			Vector3 normal = normals[line.tIndices[0]];
			for (int i = 1; i < line.tIndices.Count; i++)
			{
				if (normals[line.tIndices[i]] != normal)
				{
					delete = false;
					break;
				}
			}
			if (delete) pairsToDelete.Add(linePair);
		}

		foreach (var pair in pairsToDelete)
		{
			lines.Remove(pair.Key);
			Destroy(pair.Value.gameObject);
		}

		foreach (var line in lines.Values)
		{
			if (!listeningList.ContainsKey(line.point0))
			{
				listeningList[line.point0] = new List<IUpdatable>();
			}
			listeningList[line.point0].Add(line);

			if (!listeningList.ContainsKey(line.point1))
			{
				listeningList[line.point1] = new List<IUpdatable>();
			}
			listeningList[line.point1].Add(line);
		}
	}

	public void UpdateVertex(EditableVertex vertex, List<int> indices, Vector3 deltaPosition)
	{
		updated = true;
		deltaPosition = Quaternion.Inverse(transform.rotation) * deltaPosition;

		deltaPosition.x /= transform.localScale.x;
		deltaPosition.y /= transform.localScale.y;
		deltaPosition.z /= transform.localScale.z;

		foreach (var index in indices)
		{
			vertices[index] += deltaPosition;
		}

		// Alert update to listeners
		foreach (var child in listeningList[vertex])
		{
			child.UpdateChild();
		}
	}

	private void LateUpdate()
	{
		if (updated)
		{
			updated = false;
			mesh.vertices = vertices.ToArray();
			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
		}
	}
}