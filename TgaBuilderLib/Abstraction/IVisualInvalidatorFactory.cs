namespace TgaBuilderLib.Abstraction
{
    public interface IVisualInvalidatorFactory
    {
        IVisualInvalidator Create(object target);
    }
}
