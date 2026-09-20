namespace Tuku.Domain.ValueObjects
{
    using Newtonsoft.Json;

    public enum RegionSource
    {
        Unknown = 0,
        Recognized = 1,
        ManualBox = 2
    }

    public sealed class PageRegion
    {
        [JsonConstructor]
        public PageRegion(double x, double y, double width, double height, RegionSource source, bool isReliable)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Source = source;
            IsReliable = isReliable;
        }

        public double X { get; private set; }

        public double Y { get; private set; }

        public double Width { get; private set; }

        public double Height { get; private set; }

        public RegionSource Source { get; private set; }

        public bool IsReliable { get; private set; }

        public static Common.ValidationResult Validate(PageRegion region)
        {
            var errors = new System.Collections.Generic.List<string>();
            if (region.Width <= 0 || region.Height <= 0)
            {
                errors.Add("区域宽高必须为正数");
            }
            if (region.X < 0 || region.Y < 0 || region.X + region.Width > 1.0001 || region.Y + region.Height > 1.0001)
            {
                errors.Add("区域必须位于页面归一化坐标 [0,1] 范围内");
            }
            if (errors.Count == 0)
            {
                return Common.ValidationResult.Ok;
            }
            return Common.ValidationResult.Fail(errors);
        }
    }
}
