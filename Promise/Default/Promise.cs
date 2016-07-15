using System;
using System.Collections.Generic;

public class Promise : IPromise  {

	private IPromise<Object> promise_;
	internal static int nextPromiseId = 0;
	internal static HashSet<IPromiseInfo> pendingPromises = new HashSet<IPromiseInfo>();

	private static EventHandler<ExceptionEventArgs> unhandlerException;

	public static bool EnablePromiseTracking = false;

	public static Promise Create(){
		return new Promise ();
	}

	public static Promise Create(Action<Action, Action<Exception>> resolver){
		return new Promise (resolver);
	}

	 Promise() {
		promise_ = Promise<Object>.Create ();
		promise_.State = PromiseStateEnum.PENDING;
		if (EnablePromiseTracking)
			pendingPromises.Add(promise_);
	}

	Promise(Action<Action, Action<Exception>> resolver) : this() { 
		try {
			resolver( () => promise_.Resolve(default(Object)), ex => promise_.Reject(ex));
		}
		catch (Exception ex) {
			promise_.Reject(ex);
		}
	}

	public void Resolve() {
		promise_.Resolve (default(object));
	}

	public void Reject(Exception e) {
		promise_.Reject (e);
	}

	public IPromise Catch(Action<Exception> onRejected) {
		var resultPromise = new Promise();

		((Promise<Object>)promise_).ActionHandlers(
			resultPromise,
			v => resultPromise.Resolve(),
			ex => {
				onRejected(ex);
				resultPromise.Reject(ex);
			}
		);

		return resultPromise;
	}

	public void Done(Action onResolved, Action<Exception> onRejected) {
		Then(onResolved, onRejected)
			.Catch(ex => Promise.PropagateUnhandledException(this, ex) );
	}

	public void Done(Action onResolved) {
		Then(onResolved)
			.Catch(ex =>  Promise.PropagateUnhandledException(this, ex) );
	}

	public void Done() {
		Catch(ex =>  Promise.PropagateUnhandledException(this, ex) );
	}
		
	public IPromise Then(Func<IPromise> onResolved) {
		return Then(onResolved);
	}

	public IPromise Then(Action onResolved){
		return Then(onResolved,null);
	}

	public IPromise Then(Action onResolved, Action<Exception> onRejected) {
		var resultPromise = new Promise();
		((Promise<Object>)promise_).ActionHandlers(
			resultPromise,
			v => {
				if (onResolved != null)
					onResolved();

				resultPromise.Resolve();
			}, 
			ex => {
				if (onRejected != null)
					onRejected(ex);

				resultPromise.Reject(ex);
			});

		return (IPromise)resultPromise;
	}

	public IPromise Then(Func<IPromise> onResolved, Action<Exception> onRejected)
	{
		var resultPromise = new Promise();

		((Promise<Object>)promise_).ActionHandlers(
			resultPromise, 
			e => {
				if (onResolved != null)
					onResolved()
						.Then(
							() => resultPromise.Resolve(),
							ex => resultPromise.Reject(ex)
						);
				
				else
					resultPromise.Resolve();
			}, 
			ex => {
				if (onRejected != null)
					onRejected(ex);
		
				resultPromise.Reject(ex);
			}
		);

		return resultPromise;
	}
				
	public IPromise ThenAll(Func<IEnumerable<IPromise>> chain) {
		return Then(() => Promise.All(chain()));
	}
		
	public static IPromise All(params IPromise[] promises){
		return All((IEnumerable<IPromise>)promises); // Cast is required to force use of the other All function.
	}

	public static IPromise All(IEnumerable<IPromise> enumerable) {
		var promisesArray = UtilsPromise.ToArray (enumerable);
		if (promisesArray.Length == 0)
			return Promise.Resolved ();

		var remainingCount = promisesArray.Length;
		var resultPromise = new Promise();

		promisesArray.Each((promise, index) =>
			{
				promise
					.Catch(ex =>
						{
							if (resultPromise.promise_.State == PromiseStateEnum.PENDING)
								promise.Reject(ex);
						})
					.Then(() =>
						{
							--remainingCount;
							if (remainingCount <= 0)
								resultPromise.Resolve();
						})
					.Done();
			});

		return resultPromise;
	}

	public IPromise ThenSequence(Func<IEnumerable<Func<IPromise>>> chain) {
		return Then(() => Sequence(chain()));
	}

	public static IPromise Sequence(params Func<IPromise>[] fns) {
		return Sequence((IEnumerable<Func<IPromise>>)fns);
	}

	public static IPromise Sequence(IEnumerable<Func<IPromise>> fns) {
		return UtilsPromise.Aggregate(
			fns,
			(IPromise)Promise<object>.Resolved(default(object)),
			(prevPromise, fn) => prevPromise.Then(() => fn())
		);
	}

	public IPromise Once(Func<IEnumerable<IPromise>> chain) {
		return Then(() => Promise.Once(chain()));
	}
		
	public static IPromise Once(params IPromise[] promises){
		return Once((IEnumerable<IPromise>)promises); // Cast is required to force use of the other function.
	}

	public static IPromise Once(IEnumerable<IPromise> enumerable){
		var list = new List<IPromise> ();
		var enumerator = enumerable.GetEnumerator ();
		while (enumerator.MoveNext ()) {
			list.Add (enumerator.Current);
		}

		var promisesArray = list.ToArray();
		if (promisesArray.Length == 0)
			throw new ApplicationException("At least 1 input promise must be provided for Race");

		var resultPromise = new Promise();
		promisesArray.Each((promise) =>
			{
				promise
					.Catch(ex =>
						{
							if (resultPromise.promise_.State == PromiseStateEnum.PENDING)
								resultPromise.Reject(ex);
						})
					.Then(() =>
						{
							if (resultPromise.promise_.State == PromiseStateEnum.PENDING)
								resultPromise.Resolve();
						})
					.Done();
			});

		return resultPromise;
	}


	internal static void PropagateUnhandledException(object sender, Exception ex)
	{
		if (unhandlerException != null)
			unhandlerException(sender, new ExceptionEventArgs(ex));
	}

	public static event EventHandler<ExceptionEventArgs> UnhandledException {
		add { unhandlerException += value; }
		remove { unhandlerException -= value; }
	}

	public static IEnumerable<IPromiseInfo> GetPendingPromises(){
		return pendingPromises;
	}

	public static IPromise Resolved() {
		var promise = Promise.Create();
		promise.Resolve();
		return promise;
	}
		
}

