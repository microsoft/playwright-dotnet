
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright.Tests
{
    internal static class AssertExtensions
    {
        public static void IsEmptyOrNull(this Assert _, string str) => Assert.IsTrue(string.IsNullOrEmpty(str), "IsEmptyOrNull failed");

        public static void IsNotEmptyOrNull(this Assert _, string str) => Assert.IsFalse(string.IsNullOrEmpty(str), "IsNotEmptyOrNull failed");

        public static void IsCollectionEmpty<T>(this Assert _, IEnumerable<T> collection) => Assert.IsTrue(collection?.Count() == 0);

        public static void IsLess(this Assert _, int left, int right) => Assert.IsTrue(left < right);
        public static void IsLess(this Assert _, float left, float right) => Assert.IsTrue(left < right);

        public static void IsGreater(this Assert _, int left, int right) => Assert.IsTrue(left > right);
        public static void IsGreater(this Assert _, float left, float right) => Assert.IsTrue(left > right);

        public static void IsGreaterOrEqual(this Assert _, int left, int right) => Assert.IsTrue(left >= right);

        public static void IsGreaterOrEqual(this Assert _, float left, float right) => Assert.IsTrue(left >= right);

        public static PlaywrightCollectionAssert<T> Collection<T>(this Assert _, IEnumerable<T> collection) => new(collection);

        public class PlaywrightCollectionAssert<T>
        {
            private readonly IEnumerable<T> _collection;

            public PlaywrightCollectionAssert(IEnumerable<T> collection)
            {
                _collection = collection;
            }

            public void HasExactly(int count) => Assert.AreEqual(count, _collection?.Count());

            public void IsEmpty() => HasExactly(0);

            public void IsNotEmpty() => Assert.IsTrue(_collection?.Count() > 0);
            public void Contains(T instance) => Assert.IsTrue(_collection?.Contains(instance));

            public void DoesNotContain(T instance) => Assert.IsFalse(_collection?.Contains(instance));

            public void IsEqual(IEnumerable<T> compare)
            {
                Assert.AreEqual(compare?.Count(), _collection.Count(), "Collection.IsEqual failed.");
                CompareCollections(_collection, compare);
            }


            private void CompareCollections(IEnumerable left, IEnumerable right)
            {
                var leftEnum = left.GetEnumerator();
                var rightEnum = right.GetEnumerator();

                while (leftEnum.MoveNext() && rightEnum.MoveNext())
                {
                    if (leftEnum.Current is IEnumerable innerLeft && rightEnum.Current is IEnumerable innerRight)
                    {
                        CompareCollections(innerLeft, innerRight);
                        continue;
                    }

                    Assert.AreEqual(leftEnum.Current, rightEnum.Current, "Collection.IsEqual failed.");
                }
            }
        }
    }
}
