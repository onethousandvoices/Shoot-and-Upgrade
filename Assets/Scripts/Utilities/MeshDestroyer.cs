namespace Utilities;

public sealed class MeshDestroyer : MonoBehaviour
{
    public Mesh Mesh;

    private void OnDestroy()
    {
        if (Mesh)
            Destroy(Mesh);
    }
}