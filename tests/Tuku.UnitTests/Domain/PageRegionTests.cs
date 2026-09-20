namespace Tuku.UnitTests.Domain
{
    using System;
    using Tuku.Domain.Common;
    using Tuku.Domain.ValueObjects;
    using Xunit;

    public class PageRegionTests
    {
        [Fact]
        public void RegionWithinUnitSquare_IsValid()
        {
            var region = new PageRegion(0.1, 0.2, 0.5, 0.25, RegionSource.Recognized, true);
            Assert.True(PageRegion.Validate(region).IsValid);
        }

        [Fact]
        public void RegionOutsideUnitSquare_Fails()
        {
            var region = new PageRegion(0.8, 0.2, 0.5, 0.25, RegionSource.Recognized, true);
            var result = PageRegion.Validate(region);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void NonPositiveSize_Fails()
        {
            var region = new PageRegion(0.1, 0.2, 0, 0.25, RegionSource.ManualBox, true);
            Assert.False(PageRegion.Validate(region).IsValid);
        }
    }
}
