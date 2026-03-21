namespace Views
{
    public sealed class MainLocationView : MonoBehaviour
    {
        [SerializeField] private AstarPath _astarPath;
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private float _size = 250f;

        public AstarPath AstarPath => _astarPath;
        public float Size => _size;

        private void OnValidate()
        {
            if (!_meshFilter || !_meshFilter.sharedMesh) return;
        
            var meshSize = _meshFilter.sharedMesh.bounds.size;
            var scaleX = meshSize.x > 0f ? _size / meshSize.x : 1f;
            var scaleZ = meshSize.z > 0f ? _size / meshSize.z : 1f;
            transform.localScale = new(scaleX, 0f, scaleZ);
        }
    }
}