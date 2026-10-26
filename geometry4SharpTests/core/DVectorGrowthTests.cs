using System;
using Xunit;
using g4;

namespace geometry4SharpTests.core
{
    public class DVectorGrowthTests
    {
        [Theory]
        [InlineData(64)]
        [InlineData(256)]
        [InlineData(2048)]
        public void RepeatedSparseInsertsKeepValuesAndLogicalSize(int blockSize)
        {
            var values = new DVector<int>(blockSize);
            const int insertions = 1500;

            for (int i = 0; i < insertions; ++i)
            {
                int index = values.size + 9;
                values.insert(i + 1, index);
                Assert.Equal((i + 1) * 10, values.size);
                Assert.Equal(i + 1, values[index]);
            }

            for (int i = 0; i < insertions; ++i)
                Assert.Equal(i + 1, values[i * 10 + 9]);
        }

        [Theory]
        [InlineData(64)]
        [InlineData(256)]
        [InlineData(2048)]
        public void ShrinkThenRegrowPreservesExistingValues(int blockSize)
        {
            var values = new DVector<int>(blockSize);
            for (int i = 0; i < blockSize * 4 + 5; ++i)
                values.Add(i + 1);

            values.resize(blockSize + 3);
            Assert.Equal(blockSize + 3, values.Length);
            for (int i = 0; i < values.Length; ++i)
                Assert.Equal(i + 1, values[i]);

            values.resize(blockSize * 3 + 2);
            Assert.Equal(blockSize * 3 + 2, values.Length);

            // Overwrite newly exposed slots because DVector.resize does not
            // promise to clear data in segments that remained allocated.
            for (int i = blockSize + 3; i < values.Length; ++i)
                values[i] = 100000 + i;

            for (int i = 0; i < blockSize + 3; ++i)
                Assert.Equal(i + 1, values[i]);
            for (int i = blockSize + 3; i < values.Length; ++i)
                Assert.Equal(100000 + i, values[i]);

            values.Add(987654);
            Assert.Equal(987654, values.back);
        }

        [Theory]
        [InlineData(64)]
        [InlineData(256)]
        [InlineData(2048)]
        public void CopyAfterGrowthMaintainsLogicalContents(int blockSize)
        {
            var original = new DVector<int>(blockSize);
            for (int i = 0; i < blockSize * 3 + 17; ++i)
                original.Add(i * 17);

            var copy = new DVector<int>(original);
            Assert.Equal(original.size, copy.size);
            for (int i = 0; i < original.size; ++i)
                Assert.Equal(original[i], copy[i]);

            copy[0] = -1;
            Assert.Equal(0, original[0]);
        }
    }
}
