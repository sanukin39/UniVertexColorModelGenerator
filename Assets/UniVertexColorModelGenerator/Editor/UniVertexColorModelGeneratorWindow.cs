using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UniVertexColorModelGenerator
{
    public class UniVertexColorModelGeneratorWindow : EditorWindow
    {
        private DefaultAsset _exportDirectory = null;
        private GameObject _combineTarget = null;
        private Material _material = null; 
        private bool _exportMesh;

        [MenuItem("Window/UniVertexColorGenerateWindow")]
        static void Open()
        {
            GetWindow<UniVertexColorModelGeneratorWindow>("UniVertexColorModelGenerator").Show();
        }

        void OnGUI()
        {
            _combineTarget = (GameObject)EditorGUILayout.ObjectField("CombineTarget", _combineTarget, typeof(GameObject), true);
            _exportMesh = EditorGUILayout.Toggle("Export Mesh", _exportMesh);
            _exportDirectory = (DefaultAsset) EditorGUILayout.ObjectField("Export Directory", _exportDirectory, typeof(DefaultAsset), true);
            _material = (Material) EditorGUILayout.ObjectField("Vertex Color Material", _material, typeof(Material),
                true);
            if (GUILayout.Button("Generate"))
            {
                if (_combineTarget == null)
                {
                    return;
                }
                GenerateMesh();
            }
        }

        void GenerateMesh()
        {
            var meshFilters = _combineTarget.GetComponentsInChildren<MeshFilter>();
            var combineMeshInstances = new List<CombineInstance>();

            foreach (var meshFilter in meshFilters)
            {
                var mesh = meshFilter.sharedMesh;
                var vertices = new List<Vector3>();
                var materials = meshFilter.GetComponent<Renderer>().sharedMaterials;
                var subMeshCount = meshFilter.sharedMesh.subMeshCount;
                mesh.GetVertices(vertices);
                var colors = new Color[vertices.Count];

                for (var i = 0; i < subMeshCount; i++)
                {
                    var material = materials[i];
                    var triangles = new List<int>();
                    mesh.GetTriangles(triangles, i);

                    var materialColor = material.color;
                    for (var j = 0; j < vertices.Count; j++)
                    {
                        colors[j] = materialColor;
                    }

                    var newMesh = new Mesh
                    {
                        vertices = vertices.ToArray(), triangles = triangles.ToArray(), uv = mesh.uv,
                        normals = mesh.normals, colors = colors
                    };


                    var combineInstance = new CombineInstance
                        {transform = meshFilter.transform.localToWorldMatrix, mesh = newMesh};
                    combineMeshInstances.Add(combineInstance);
                }
            }

            _combineTarget.SetActive(false);

            GenerateObject(combineMeshInstances);
        }

        void GenerateObject(List<CombineInstance> instances)
        {
            var outputObjectName = "output"; 
            var newObject = new GameObject(outputObjectName);

            var meshRenderer = newObject.AddComponent<MeshRenderer>();
            var meshFilter = newObject.AddComponent<MeshFilter>();

            meshRenderer.material = _material;
            var mesh = new Mesh();
            mesh.CombineMeshes(instances.ToArray());
            Unwrapping.GenerateSecondaryUVSet(mesh);

            meshFilter.sharedMesh = mesh;
            newObject.transform.parent = _combineTarget.transform.parent;

            if (!_exportMesh || _exportDirectory == null)
            {
                return;
            }

            ExportMesh(mesh, outputObjectName);
        }

        void ExportMesh(Mesh mesh, string fileName)
        {
            var exportDirectoryPath = AssetDatabase.GetAssetPath(_exportDirectory);
            if (Path.GetExtension(fileName) != ".asset")
            {
                fileName += ".asset";
            }
            var exportPath = Path.Combine(exportDirectoryPath, fileName);
            AssetDatabase.CreateAsset(mesh, exportPath);
        }
    }
}
