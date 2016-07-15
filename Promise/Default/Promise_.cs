using System;
using System.Collections.Generic;

public class Promise_ : Promise<Object>, IPromise_ {

	internal static int nextPromiseId = 0;
	internal static HashSet<IPromiseInfo> pendingPromises = new HashSet<IPromiseInfo>();

	private static EventHandler<ExceptionEventArgs> unhandlerException;

	public static bool EnablePromiseTracking = false;

	public static IPromise_ Create(){
		return new Promise_ ();
	}

	Promise_() {
		this.State = PromiseStateEnum.PENDING;
		if (EnablePromiseTracking)
			pendingPromises.Add(this);
	}

	public Promise_(Action<Action, Action<Exception>> resolver) : this() { 
		try {
			resolver( () => base.Resolve(default(Object)), ex => Reject(ex));
		}
		catch (Exception ex) {
			Reject(ex);
		}
	}

	public void Resolve() {
		base.Resolve (default(object));
	}

	public IPromise_ Catch(Action<Exception> onRejected) {
		var resultPromise = new Promise_();
			
		this.ActionHandlers(
			resultPromise,
			v => resultPromise.Resolve(v),
			ex => {
				onRejected(ex);
				resultPromise.Reject(ex);
			}
		);

		return resultPromise;
	}

	public void Done(Action onResolved, Action<Exception> onRejected) {
		Then(onResolved, onRejected)
			.Catch(ex => Promise_.PropagateUnhandledException(this, ex) );
	}

	public void Done(Action onResolved) {
		Then(onResolved)
			.Catch(ex =>  Promise_.PropagateUnhandledException(this, ex) );
	}
		
	public IPromise_ Then(Func<IPromise_> onResolved) {
		return Then(onResolved);
	}

	public IPromise_ Then(Action onResolved){
		return Then(onResolved,null);
	}

	public IPromise_ Then(Action onResolved, Action<Exception> onRejected) {
		var resultPromise = new Promise_();
		ActionHandlers(resultPromise,
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

		return (IPromise_)resultPromise;
	}

	public IPromise_ Then(Func<IPromise_> onResolved, Action<Exception> onRejected)
	{
		var resultPromise = new Promise_();
	
		base.ActionHandlers(resultPromise, 
			(e) => {
				if (onResolved != null)
					onResolved()
						.Then(
							() => resultPromise.Resolve(default(object)),
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
				
	public IPromise_ ThenAll(Func<IEnumerable<IPromise_>> chain) {
		return Then(() => Promise_.All(chain()));
	}
		
	public static IPromise_ All(params IPromise_[] promises){
		return All((IEnumerable<IPromise_>)promises); // Cast is required to force use of the other All function.
	}

	public static IPromise_ All(IEnumerable<IPromise_> enumerable) {
		var promisesArray = UtilsPromise.ToArray (enumerable);
		if (promisesArray.Length == 0)
			return Promise_.Resolved ();

		var remainingCount = promisesArray.Length;
		var resultPromise = new Promise_();

		promisesArray.Each((promise, index) =>
			{
				promise
					.Catch(ex =>
						{
							if (resultPromise.State == PromiseStateEnum.PENDING)
								resultPromise.Reject(ex);
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

	public IPromise_ ThenSequence(Func<IEnumerable<Func<IPromise_>>> chain) {
		return Then(() => Sequence(chain()));
	}

	public static IPromise_ Sequence(params Func<IPromise_>[] fns) {
		return Sequence((IEnumerable<Func<IPromise_>>)fns);
	}

	public static IPromise_ Sequence(IEnumerable<Func<IPromise_>> fns) {
		return UtilsPromise.Aggregate(
			fns,
			(IPromise_)Promise<object>.Resolved(default(object)),
			(prevPromise, fn) => prevPromise.Then(() => fn())
		);
	}

	public IPromise_ Once(Func<IEnumerable<IPromise_>> chain) {
		return Then(() => Promise_.Once(chain()));
	}
		
	public static IPromise_ Once(params IPromise_[] promises){
		return Once((IEnumerable<IPromise_>)promises); // Cast is required to force use of the other function.
	}

	public static IPromise_ Once(IEnumerable<IPromise_> enumerable){
		var list = new List<IPromise_> ();
		var enumerator = enumerable.GetEnumerator ();
		while (enumerator.MoveNext ()) {
			list.Add (enumerator.Current);
		}

		var promisesArray = list.ToArray();
		if (promisesArray.Length == 0)
			throw new ApplicationException("At least 1 input promise must be provided for Race");

		var resultPromise = new Promise_();
		promisesArray.Each((promise) =>
			{
				promise
					.Catch(ex =>
						{
							if (resultPromise.State == PromiseStateEnum.PENDING)
								resultPromise.Reject(ex);
						})
					.Then(() =>
						{
							if (resultPromise.State == PromiseStateEnum.PENDING)
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

	public static IPromise_ Resolved() {
		var promise = new Promise<object>();
		promise.Resolve(default(object));
		return (IPromise_)promise;
	}
		
}

