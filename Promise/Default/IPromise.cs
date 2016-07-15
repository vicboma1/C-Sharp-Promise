using System;
using System.Collections.Generic;

public interface IPromise : IResolvable
{
	/// <summary>
	/// Completes the promise. 
	/// onResolved is called on successful completion.
	/// onRejected is called on error.
	/// </summary>
	void Done(Action onResolved, Action<Exception> onRejected);

	/// <summary>
	/// Completes the promise. 
	/// onResolved is called on successful completion.
	/// Adds a default error handler.
	/// </summary>
	void Done(Action onResolved);

	/// <summary>
	/// Complete the promise. Adds a default error handler.
	/// </summary>
	void Done();

	/// <summary>
	/// Handle errors for the promise. 
	/// </summary>
	IPromise Catch(Action<Exception> onRejected);

	/// <summary>
	/// Add a resolved callback that chains a non-value promise.
	/// </summary>
	IPromise Then(Func<IPromise> onResolved);

	/// <summary>
	/// Add a resolved callback.
	/// </summary>
	IPromise Then(Action onResolved);

	/// <summary>
	/// Add a resolved callback and a rejected callback.
	/// The resolved callback chains a non-value promise.
	/// </summary>
	IPromise Then(Func<IPromise> onResolved, Action<Exception> onRejected);

	/// <summary>
	/// Add a resolved callback and a rejected callback.
	/// </summary>
	IPromise Then(Action onResolved, Action<Exception> onRejected);

	/// <summary>
	/// Chain an enumerable of promises, all of which must resolve.
	/// The resulting promise is resolved when all of the promises have resolved.
	/// It is rejected as soon as any of the promises have been rejected.
	/// </summary>
	IPromise ThenAll(Func<IEnumerable<IPromise>> chain);

	/// <summary>
	/// Chain a sequence of operations using promises.
	/// Reutrn a collection of functions each of which starts an async operation and yields a promise.
	/// Each function will be called and each promise resolved in turn.
	/// The resulting promise is resolved after each promise is resolved in sequence.
	/// </summary>
	IPromise ThenSequence(Func<IEnumerable<Func<IPromise>>> chain);

	/// <summary>
	/// Takes a function that yields an enumerable of promises.
	/// Returns a promise that resolves when the first of the promises has resolved.
	/// </summary>
	IPromise Once(Func<IEnumerable<IPromise>> chain);
}
