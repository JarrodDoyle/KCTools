namespace KeepersCompound.Formats;

public interface IFromType<out T, in T2>
{
    public static abstract T FromType(T2 p);
}