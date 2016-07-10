using System;
using System.Collections.Generic;

public static class EnumerablePromise
{
	public static IEnumerable<T> _Empty<T>() {
		return new T[0];
	}

	public static IEnumerable<T> _LazyEach<T>(this IEnumerable<T> source, Action<T> action) {
		foreach (var item in source) {
			action.Invoke(item);
			yield return item;
		}
	}

	public static void _Each<T>(this IEnumerable<T> source, Action<T> action) {
		foreach (var item in source)
			action.Invoke(item);
	}

	public static void _Each<T>(this IEnumerable<T> source, Action<T, int> action) {
		var index = 0;
		foreach (T item in source){
			action.Invoke(item, index);
			index++;
		}
	}
}

