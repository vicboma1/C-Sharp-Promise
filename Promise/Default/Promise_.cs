using System;
using System.Collections.Generic;

public class Promise_ : IPromise_, IResolvable_, IPromiseInfo {


	internal static int nextPromiseId = 0;
	internal static HashSet<IPromiseInfo> pendingPromises = new HashSet<IPromiseInfo>();
	internal static MappingStatePromise mappingStatePromise = MappingStatePromise.Create();

	private static EventHandler<ExceptionEventArgs> unhandlerException;

	public static bool EnablePromiseTracking = false;

	private Exception rejectionException;

	public int Id { get; private set; }
	public string Name { get; private set; }
	public PromiseStateEnum State { get; private set; }

	public static event EventHandler<ExceptionEventArgs> UnhandledException {
		add { unhandlerException += value; }
		remove { unhandlerException -= value; }
	}

	public static IEnumerable<IPromiseInfo> GetPendingPromises(){
		return pendingPromises;
	}

	public Promise_() {
		
		this.State = PromiseStateEnum.PENDING;
		if (EnablePromiseTracking)
			pendingPromises.Add(this);
	}

	public Promise_(Action<Action, Action<Exception>> resolver) : this() { 

		try {
			resolver( () => Resolve(), ex => Reject(ex));
		}
		catch (Exception ex) {
			Reject(ex);
		}
	}
		

	public void Reject(Exception ex) {
		if (State != PromiseStateEnum.PENDING)
			throw new ApplicationException("Attempt to reject a promise that is already in state: " + State + ", a promise can only be rejected when it is still in state: " + PromiseStateEnum.PENDING);

		rejectionException = ex;
		State = PromiseStateEnum.REJECTED;

		if (EnablePromiseTracking)
			pendingPromises.Remove(this);

		mappingStatePromise.InvokeRejectHandlers(ex);            
	}


	public void Resolve() {
		if (State != PromiseStateEnum.PENDING)
			throw new ApplicationException("Attempt to reject a promise that is already in state: " + State + ", a promise can only be rejected when it is still in state: " + PromiseStateEnum.PENDING);


		State = PromiseStateEnum.RESOLVED;

		if (EnablePromiseTracking)
			pendingPromises.Remove(this);

		mappingStatePromise.InvokeResolveHandlers();
	}

	public void Done(Action onResolved, Action<Exception> onRejected) {
		Then(onResolved, onRejected)
			.Catch(ex =>
				Promise_.PropagateUnhandledException(this, ex)
			);
	}

	public void Done(Action onResolved) {
		Then(onResolved)
			.Catch(ex => 
				Promise_.PropagateUnhandledException(this, ex)
			);
	}

	public void Done() {
		Catch(ex => Promise_.PropagateUnhandledException(this, ex));
	}

	public IPromise_ Catch(Action<Exception> onRejected){
		var resultPromise = new Promise_();

		Action resolveHandler = () =>
		{
			resultPromise.Resolve();
		};

		Action<Exception> rejectHandler = ex =>
		{
			onRejected(ex);

			resultPromise.Reject(ex);
		};

		ActionHandlers(resultPromise, resolveHandler, rejectHandler);

		return resultPromise;
	}

	public IPromise<X> Then<X>(Func<IPromise<X>> onResolved) {
		return Then(onResolved, null);
	}

	public IPromise_ Then(Func<IPromise_> onResolved) {
		return Then(onResolved, null);
	}

	public IPromise_ Then(Action onResolved){
		return Then(onResolved, null);
	}

	public IPromise<Z> Then<Z>(Func<IPromise<Z>> onResolved, Action<Exception> onRejected) {
		var resultPromise = new Promise<Z>();

		Action resolveHandler = () =>
		{
			onResolved()
				.Then(
					(Z chainedValue) => resultPromise.Resolve(chainedValue),
					ex => resultPromise.Reject(ex)
				);
		};

		Action<Exception> rejectHandler = ex =>
		{
			if (onRejected != null)
				onRejected(ex);

			resultPromise.Reject(ex);
		};

		ActionHandlers(resultPromise, resolveHandler, rejectHandler);

		return resultPromise;
	}

	public IPromise_ Then(Func<IPromise_> onResolved, Action<Exception> onRejected)
	{
		var resultPromise = new Promise_();

		Action resolveHandler = () =>
		{
			if (onResolved != null)
			{
				onResolved()
					.Then(
						() => resultPromise.Resolve(),
						ex => resultPromise.Reject(ex)
					);
			}
			else
			{
				resultPromise.Resolve();
			}
		};

		Action<Exception> rejectHandler = ex =>
		{
			if (onRejected != null)
			{
				onRejected(ex);
			}

			resultPromise.Reject(ex);
		};

		ActionHandlers(resultPromise, resolveHandler, rejectHandler);

		return resultPromise;
	}

	public IPromise_ Then(Action onResolved, Action<Exception> onRejected) {
		var resultPromise = new Promise_();

		Action resolveHandler = () =>
		{
			if (onResolved != null)
				onResolved();

			resultPromise.Resolve();
		};

		Action<Exception> rejectHandler = ex =>
		{
			if (onRejected != null)
				onRejected(ex);

			resultPromise.Reject(ex);
		};

		ActionHandlers(resultPromise, resolveHandler, rejectHandler);

		return resultPromise;
	}

	private void ActionHandlers(IRejectable resultPromise, Action resolveHandler, Action<Exception> rejectHandler) {
		mappingStatePromise
			.Get (State)
			.Invoke (resultPromise, resolveHandler, rejectHandler, rejectionException);
	}

	public IPromise_ ThenAll(Func<IEnumerable<IPromise_>> chain) {
		return Then(() => Promise_.All(chain()));
	}

	public IPromise<IEnumerable<ConvertedT>> ThenAll<ConvertedT>(Func<IEnumerable<IPromise<ConvertedT>>> chain) {
		return Then(() => Promise<ConvertedT>.All(chain()));
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
			Promise_.Resolved(),
			(prevPromise, fn) => {
				return prevPromise.Then(() => fn());
			}
		);
	}

	public IPromise_ Once(Func<IEnumerable<IPromise_>> chain) {
		return Then(() => Promise_.Once(chain()));
	}

	public IPromise<T> Once<T>(Func<IEnumerable<IPromise<T>>> chain) {
		return Then(() => Promise<T>.Once(chain()));
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
							if (resultPromise.State == PromiseStateEnum.PENDING)
								resultPromise.Resolve();
						})
					.Done();
			});

		return resultPromise;
	}

	public static IPromise_ Resolved(){
		var promise = new Promise_ ();
		promise.Resolve();
		return promise;
	}

	public static IPromise_ Rejected(Exception ex) {
		var promise = new Promise_ ();
		promise.Reject(ex);

		return promise;
	}

	internal static void PropagateUnhandledException(object sender, Exception ex)
	{
		if (unhandlerException != null)
			unhandlerException(sender, new ExceptionEventArgs(ex));
	}
}

