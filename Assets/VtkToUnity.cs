using UnityEngine;
using System.Collections;

public class VtkToUnity
{
	Mesh mesh;
	public GameObject go;
	Kitware.VTK.vtkPolyDataAlgorithm pda;
	Kitware.VTK.vtkTriangleFilter triangleFilter;
	string name;

	public VtkToUnity(Kitware.VTK.vtkPolyDataAlgorithm pda, string name)
	{
		this.name = name;
		this.pda = pda;
		this.mesh = new Mesh();
		triangleFilter = Kitware.VTK.vtkTriangleFilter.New();
		triangleFilter.SetInputConnection(pda.GetOutputPort());
		CreateGameObject();
	}

	public VtkToUnity(string filename)
	{
		string filepath = Application.dataPath + "/" + filename;
		filepath = filepath.Replace("/", "\\");
		Kitware.VTK.vtkXMLPolyDataReader reader = Kitware.VTK.vtkXMLPolyDataReader.New();
		if (reader.CanReadFile(filepath) == 0)
		{
			Debug.Log(filepath + " could not be loaded by Vtk!");
			return;
		}
		name = filename;
		reader.SetFileName(filepath);
		this.mesh = new Mesh();
		triangleFilter = Kitware.VTK.vtkTriangleFilter.New();
		triangleFilter.SetInputConnection(reader.GetOutputPort());
		CreateGameObject();
	}

	public void Update()
	{
		PolyDataToMesh();
	}

	GameObject CreateGameObject()
	{
		PolyDataToMesh();

		go = new GameObject(name);
		MeshFilter meshFilter = go.AddComponent<MeshFilter>();
		meshFilter.sharedMesh = mesh;
		MeshRenderer renderer = go.AddComponent<MeshRenderer>();
		Material mat = new Material(Shader.Find("Diffuse"));
		renderer.material = mat;

		return go;
	}

	void PolyDataToMesh()
	{
		// mesh.MarkDynamic();
		mesh.Clear();

		triangleFilter.Update();
		Kitware.VTK.vtkPolyData pd = triangleFilter.GetOutput();

		// Points / Vertices
		int numPoints = pd.GetNumberOfPoints();
		Vector3[] vertices = new Vector3[numPoints];
		for (int i = 0; i < numPoints; ++i)
		{
			double[] pnt = pd.GetPoint(i);
			// Flip z-up to y-up
			vertices[i] = new Vector3(-(float)pnt[0], (float)pnt[2], (float)pnt[1]);
		}
		mesh.vertices = vertices;

		// Triangles / Cells
		int numTriangles = pd.GetNumberOfPolys();
		int[] triangles = new int[numTriangles * 3];
		Kitware.VTK.vtkIdList pts = Kitware.VTK.vtkIdList.New();
		int prim = 0;
		Kitware.VTK.vtkCellArray cells = pd.GetPolys();
		if (cells.GetNumberOfCells() > 0)
		{
			cells.InitTraversal();
			while (cells.GetNextCell(pts) != 0)
			{
				for (int i = 0; i < pts.GetNumberOfIds(); ++i)
					triangles[prim * 3 + i] = pts.GetId(i);

				++prim;
			}
		}
		mesh.triangles = triangles;

		// Texture coordinates
		Vector2[] uvs;
		int numCoords = 0;
		Kitware.VTK.vtkDataArray vtkTexCoords = pd.GetPointData().GetTCoords();
		if (vtkTexCoords != null)
		{
			numCoords = vtkTexCoords.GetNumberOfTuples();
			uvs = new Vector2[numCoords];
			for (int i = 0; i < numCoords; ++i)
			{
				double[] texCoords = vtkTexCoords.GetTuple2(i);
				uvs[i] = new Vector2((float)texCoords[0], (float)texCoords[1]);
			}
			mesh.uv = uvs;
		}

		Debug.Log(name + " - Vertices: " + numPoints + ", triangle: " + numTriangles + ", UVs: " + numCoords);

		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		//mesh.Optimize();
	}
}
