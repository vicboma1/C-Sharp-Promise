using System.Collections;
using System.Collections.Generic;
using System;

public interface IPromise<T> : IResolvable<T>, IPromiseInfo
{ 
	/// <summary>
	/// Completes the promise. 
	/// onResolved is called on successful completion.
	/// onRejected is called on error.
	/// </summary>
	void Done(Action<T> onResolved, Action<Exception> onRejected);

	/// <summary>
	/// Completes the promise. 
	/// onResolved is called on successful completion.
	/// Adds a default error handler.
	/// </summary>
	void Done(Action<T> onResolved);

	/// <summary>
	/// Complete the promise. Adds a default error handler.
	/// </summary>
	void Done();

	/// <summary>
	/// Handle errors for the promise. 
	/// </summary>
	IPromise<T> Catch(Action<Exception> onRejected);

	/// <summary>
	/// Add a resolved callback that chains a value promise (optionally converting to a different value type).
	/// </summary>
	IPromise<Z> Then<Z>(Func<T, IPromise<Z>> onResolved);

	/// <summary>
	/// Add a resolved callback.
	/// </summary>
	IPromise<T> Then(Action<T> onResolved);

	/// <summary>
	/// Add a resolved callback and a rejected callback.
	/// The resolved callback chains a value promise (optionally converting to a different value type).
	/// </summary>
	IPromise<Z> Then<Z>(Func<T, IPromise<Z>> onResolved, Action<Exception> onRejected);

	/// <summary>
	/// Add a resolved callback and a rejected callback.
	/// </summary>
	IPromise<T> Then(Action<T> onResolved, Action<Exception> onRejected);

	/// <summary>
	/// Return a new promise with a different value.
	/// May also change the type of the value.
	/// </summary>
	IPromise<Z> Then<Z>(Func<T, Z> transform);

	/// <summary>
	/// Chain an enumerable of promises, all of which must resolve.
	/// Returns a promise for a collection of the resolved results.
	/// The resulting promise is resolved when all of the promises have resolved.
	/// It is rejected as soon as any of the promises have been rejected.
	/// </summary>
	IPromise<IEnumerable<Z>> ThenAll<Z>(Func<T, IEnumerable<IPromise<Z>>> chain);

	/// <summary>
	/// Takes a function that yields an enumerable of promises.
	/// Returns a promise that resolves when the first of the promises has resolved.
	/// Yields the value from the first promise that has resolved.
	/// </summary>
	IPromise<Z> Once<Z>(Func<T, IEnumerable<IPromise<Z>>> chain);


}

