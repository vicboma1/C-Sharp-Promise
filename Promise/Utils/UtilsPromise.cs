using System;
using System.Collections.Generic;

public class UtilsPromise
{
	public static TAccumulate Aggregate<TSource, TAccumulate>(IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func) {
		var result = seed;
		foreach (var element in source)
			result = func(result, element);
		return result;
	}

	public static T[] ToArray<T>(IEnumerable<T> enumerable){
		var list = new List<T> ();
		using (var e = enumerable.GetEnumerator ()) {
			while (e.MoveNext ())
				list.Add (e.Current);
		}

		return list.ToArray();
	}
}

