namespace WolfensteinInfinite
{
    public interface ISprite
    {
        public void Update(float frameTimeSeconds);
        public Texture32 GetTexture(float angle);
    }
}
