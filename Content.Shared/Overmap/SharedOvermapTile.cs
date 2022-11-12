namespace Content.Shared.Overmap;

public abstract class SharedOvermapTile
{
    public static float TileSize = 4_000f;
    public static Matrix3 InveBottomLeftOrigin = BottomLeftOrigin.Invert();
    public static Matrix3 BottomLeftOrigin => Matrix3.CreateTranslation(TileSize / 2, TileSize / 2);

    public static Matrix3 GetWorldMatrix(Vector2 position)
    {
        return Matrix3.CreateTranslation(
            TileSize * position.X,
            TileSize * position.Y
        ) * BottomLeftOrigin;
    }

    public static Matrix3 GetInvWorldMatrix(Vector2 position)
    {
        return InveBottomLeftOrigin * Matrix3.CreateTranslation(
            TileSize * position.X,
            TileSize * position.Y
        ).Invert();
    }
}
