using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

public class PromiseTests
{
	public PromiseTests(){
	
		this.GetType ()
			.GetMethods (BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
			.ToList ()
			.ForEach (x => x.Invoke(this, new object[] { }));
	}
		
		private static IPromise<T> Factory<T>(){
			return Promise<T>.Create ();
		}

		public void can_resolve_simple_promise()
		{
			var promisedValue = 5;
			var promise = Promise<int>.Resolved(promisedValue);

			var completed = 0;
			promise.Then(v =>
				{
				Assert.Equal(promisedValue, v);
				++completed;
				});

			Assert.Equal(1, completed);
		}

		
		public void can_reject_simple_promise()
		{
			var ex = new Exception();
			var promise = Promise<int>.Rejected(ex);

			var errors = 0;
			promise.Catch(e =>
				{
					Assert.Equal(ex, e);
					++errors;
				});

			Assert.Equal(1, errors);
		}

		
		public void exception_is_thrown_for_reject_after_reject()
		{
			var promise = Factory<int>();

			promise.Reject(new ApplicationException());

			Assert.Throws<ApplicationException>(() =>
				promise.Reject(new ApplicationException())
			);
		}

		
		public void exception_is_thrown_for_reject_after_resolve()
		{
			var promise = Factory<int>();
			promise.Resolve(5);

			Assert.Throws<ApplicationException>(() =>
				promise.Reject(new ApplicationException())
			);
		}

		
		public void exception_is_thrown_for_resolve_after_reject()
		{
			var promise = Factory<int>();	
			promise.Reject(new ApplicationException());

			Assert.Throws<ApplicationException>(() =>
				promise.Resolve(5)
			);
		}

		
		public void can_resolve_Promiseand_trigger_then_handler()
		{
			var promise = Factory<int>();
			var completed = 0;
			var promisedValue = 15;

			promise.Then(v =>
				{
					Assert.Equal(promisedValue, v);
					++completed;
				});

			promise.Resolve(promisedValue);

			Assert.Equal(1, completed);
		}

		
		public void exception_is_thrown_for_resolve_after_resolve()
		{
			var promise = Factory<int>();
			promise.Resolve(5);

			Assert.Throws<ApplicationException>(() =>
				promise.Resolve(5)
			);
		}

		
		public void can_resolve_Promiseand_trigger_multiple_then_handlers_in_order()
		{
			var promise = Factory<int>();
			var completed = 0;

			promise
				.Then(v => Assert.Equal(1, ++completed))
				.Then(v => Assert.Equal(2, ++completed));

			promise.Resolve(1);

			Assert.Equal(2, completed);
		}

		
		public void can_resolve_Promiseand_trigger_then_handler_with_callback_registration_after_resolve()
		{
			var promise = Factory<int>();
			var completed = 0;
			var promisedValue = -10;

			promise.Resolve(promisedValue);

			promise.Then(v => 
				{
					Assert.Equal(promisedValue, v);
					++completed;
				});

			Assert.Equal(1, completed);
		}

		
		public void can_reject_Promiseand_trigger_error_handler()
		{
			var promise = Factory<int>();
			var ex = new ApplicationException();
			var completed = 0;
			promise.Catch(e =>
				{
					Assert.Equal(ex, e);
					++completed;
				});

			promise.Reject(ex);

			Assert.Equal(1, completed);
		}

		
		public void can_reject_Promiseand_trigger_multiple_error_handlers_in_order()
		{
			var promise = Factory<int>();
			var ex = new ApplicationException();
			var completed = 0;

			promise.Catch(e =>
				{
					Assert.Equal(ex, e);
					Assert.Equal(1, ++completed);
				})
				.Catch(e =>
				{
					Assert.Equal(ex, e);
					Assert.Equal(2, ++completed);
				});

			promise.Reject(ex);

			Assert.Equal(2, completed);
		}

		
		public void can_reject_Promiseand_trigger_error_handler_with_registration_after_reject()
		{
			var promise = Factory<int>();
			var ex = new ApplicationException();
			promise.Reject(ex);

			var completed = 0;
			promise.Catch(e =>
				{
					Assert.Equal(ex, e);
					++completed;
				});

			Assert.Equal(1, completed);
		}

		
		public void error_handler_is_not_invoked_for_resolved_promised()
		{
			var promise = Factory<int>();	
			promise.Catch(e =>
				{
					throw new ApplicationException("This shouldn't happen");
				});

			promise.Resolve(5);
		}

		
		public void then_handler_is_not_invoked_for_rejected_promise()
		{
			var promise = Factory<int>();
			promise.Then(v =>
				{
					throw new ApplicationException("This shouldn't happen");
				});

			promise.Reject(new ApplicationException("Rejection!"));
		}

		
		public void chain_multiple_promises_using_all()
		{
			var promise = Factory<String>();
			var chainedPromise1 = Promise<int>.Create ();
			var chainedPromise2 = Promise<int>.Create ();
			var chainedResult1 = 10;
			var chainedResult2 = 15;

			var completed = 0;

			promise
			.ThenAll(i => new List<IPromise<int>>() { chainedPromise1,chainedPromise2 } )
				.Then(result =>
					{
						var items = result.ToArray();
						Assert.Equal(2, items.Length);
						Assert.Equal(chainedResult1, items[0]);
						Assert.Equal(chainedResult2, items[1]);

						++completed;
					});

			Assert.Equal(0, completed);

			promise.Resolve("hello");

			Assert.Equal(0, completed);

			chainedPromise1.Resolve(chainedResult1);

			Assert.Equal(0, completed);

			chainedPromise2.Resolve(chainedResult2);

			Assert.Equal(1, completed);
		}


		
		public void chain_multiple_promises_using_all_that_are_resolved_out_of_order()
		{
		var promise = (Promise<String>)Factory<String> ();			
		    var chainedPromise1 = Promise<int>.Create ();
			var chainedPromise2 = Promise<int>.Create ();
			var chainedResult1 = 10;
			var chainedResult2 = 15;

			var completed = 0;

			promise
				.ThenAll(i => LinqExts.FromItems(chainedPromise1, chainedPromise2).Cast<IPromise<int>>())
				.Then(result =>
					{
						var items = result.ToArray();
						Assert.Equal(2, items.Length);
						Assert.Equal(chainedResult1, items[0]);
						Assert.Equal(chainedResult2, items[1]);

						++completed;
					});

			Assert.Equal(0, completed);

			promise.Resolve("hello");

			Assert.Equal(0, completed);

			chainedPromise2.Resolve(chainedResult2);

			Assert.Equal(0, completed);

			chainedPromise1.Resolve(chainedResult1);

			Assert.Equal(1, completed);
		}

		
		public void chain_multiple_promises_using_all_and_convert_to_non_value_promise()
		{
			var promise = Factory<string>();
	 		var chainedPromise1 = Promise.Create ();
			var chainedPromise2 = Promise.Create ();

			var completed = 0;

			promise
				.ThenAll(i => LinqExts.FromItems(chainedPromise1, chainedPromise2).Cast<IPromise<string>>())
				.Then((str) =>
					{
						++completed;
					});

			Assert.Equal(0, completed);

			promise.Resolve("hello");

			Assert.Equal(0, completed);

			chainedPromise1.Resolve();

			Assert.Equal(0, completed);

			chainedPromise2.Resolve();

			Assert.Equal(1, completed);
		}

		
		public void combined_Promiseis_resolved_when_children_are_resolved()
		{
			var promise1 = Factory<int>();
			var promise2 = Factory<int>();

			var all = Promise<int>.All(LinqExts.FromItems<IPromise<int>>(promise1, promise2));

			var completed = 0;

			all.Then(v =>
				{
					++completed;

					var values = v.ToArray();
					Assert.Equal(2, values.Length);
					Assert.Equal(1, values[0]);
					Assert.Equal(2, values[1]);
				});

			promise1.Resolve(1);
			promise2.Resolve(2);

			Assert.Equal(1, completed);
		}

		
		public void combined_Promiseis_rejected_when_first_Promiseis_rejected()
		{
			var promise1 = Factory<int>();
			var promise2 = Factory<int>();

			var all = Promise<int>.All(LinqExts.FromItems<IPromise<int>>(promise1, promise2));

			all.Then(v =>
				{
					throw new ApplicationException("Shouldn't happen");
				});

			var errors = 0;
			all.Catch(e =>
				{
					++errors;
				});

			promise1.Reject(new ApplicationException("Error!"));
			promise2.Resolve(2);

			Assert.Equal(1, errors);
		}

		
		public void combined_Promiseis_rejected_when_second_Promiseis_rejected()
		{
			var promise1 = Factory<int>();
			var promise2 = Factory<int>();

			var all = Promise<int>.All(LinqExts.FromItems<IPromise<int>>(promise1, promise2));

			all.Then(v =>
				{
					throw new ApplicationException("Shouldn't happen");
				});

			var errors = 0;
			all.Catch(e =>
				{
					++errors;
				});

			promise1.Resolve(2);
			promise2.Reject(new ApplicationException("Error!"));

			Assert.Equal(1, errors);
		}

		
		public void combined_Promiseis_rejected_when_both_promises_are_rejected()
		{
			var promise1 = Factory<int>();
			var promise2 = Factory<int>();

			var all = Promise<int>.All(LinqExts.FromItems<IPromise<int>>(promise1, promise2));

			all.Then(v =>
				{
					throw new ApplicationException("Shouldn't happen");
				});

			var errors = 0;
			all.Catch(e =>
				{
					++errors;
				});

			promise1.Reject(new ApplicationException("Error!"));
			promise2.Reject(new ApplicationException("Error!"));

			Assert.Equal(1, errors);
		}

		
		public void combined_Promiseis_resolved_if_there_are_no_promises()
		{
			var all = Promise<int>.All(LinqExts.Empty<IPromise<int>>());

			var completed = 0;

			all.Then(v =>
				{
					++completed;

					Assert.Empty(v);
				});

			Assert.Equal(1, completed);
		}

		
		public void combined_Promiseis_resolved_when_all_promises_are_already_resolved()
		{
			var promise1 = Promise<int>.Resolved(1);
			var promise2 = Promise<int>.Resolved(1);

			var all = Promise<int>.All(LinqExts.FromItems(promise1, promise2));

			var completed = 0;

			all.Then(v =>
				{
					++completed;

					Assert.Empty(v);
				});

			Assert.Equal(1, completed);
		}

		
		public void can_transform_Promisevalue()
		{
			var promise = Factory<int>();
	
			var promisedValue = 15;
			var completed = 0;

			promise
				.Then(v => v.ToString())
				.Then(v =>
					{
						Assert.Equal(promisedValue.ToString(), v);

						++completed;
					});

			promise.Resolve(promisedValue);

			Assert.Equal(1, completed);           
		}

		
		public void rejection_of_source_Promiserejects_transformed_promise()
		{
			var promise = Factory<int>();

			var ex = new Exception();
			var errors = 0;

			promise
				.Then(v => v.ToString())
				.Catch(e =>
					{
						Assert.Equal(ex, e);

						++errors;
					});

			promise.Reject(ex);

			Assert.Equal(1, errors);
		}

		
		public void exception_thrown_during_transform_rejects_transformed_promise()
		{
			var promise = Factory<int>();

			var promisedValue = 15;
			var errors = 0;
			var ex = new Exception();

			promise
				.Then(v => 
					{
						throw ex;
					})
				.Catch(e =>
					{
						Assert.Equal(ex, e);

						++errors;
					});

			promise.Resolve(promisedValue);

			Assert.Equal(1, errors);
		}

		
		public void can_chain_Promiseand_convert_type_of_value()
		{
			var promise = Factory<int>();
			var chainedPromise = Factory<string>();

			var promisedValue = 15;
			var chainedPromiseValue = "blah";
			var completed = 0;

			promise
				.Then<string>(v => chainedPromise)
				.Then(v =>
					{
						Assert.Equal(chainedPromiseValue, v);

						++completed;
					});

			promise.Resolve(promisedValue);
			chainedPromise.Resolve(chainedPromiseValue);

			Assert.Equal(1, completed);
		}

		
		public void can_chain_Promiseand_convert_to_non_value_promise()
		{
			var promise = Factory<int>();
			var chainedPromise = Promise.Create();

			var promisedValue = 15;
			var completed = 0;

			promise
				.Then(v => (IPromise)chainedPromise)
				.Then((str) =>
					{
						++completed;
					});

			promise.Resolve(promisedValue);
			chainedPromise.Resolve();

			Assert.Equal(1, completed);
		}

		
		public void exception_thrown_in_chain_rejects_resulting_promise()
		{
			var promise = Factory<int>();

			var ex = new Exception();
			var errors = 0;

			promise
				.Then(v =>
					{
						throw ex;
					})
				.Catch(e =>
					{
						Assert.Equal(ex, e);

						++errors;
					});

			promise.Resolve(15);

			Assert.Equal(1, errors);
		}

		
		public void rejection_of_source_Promiserejects_chained_promise()
		{
			var promise = Factory<int>();
			var chainedPromise = Factory<string>();

			var ex = new Exception();
			var errors = 0;

			promise
				.Then<string>(v => chainedPromise)
				.Catch(e =>
					{
						Assert.Equal(ex, e);

						++errors;
					});

			promise.Reject(ex);

			Assert.Equal(1, errors);
		}

		
		public void race_is_resolved_when_first_Promiseis_resolved_first()
		{
			var promise1 = Factory<int>(); 
			var promise2 = Factory<int>();

			var resolved = 0;

			Promise<int>
				.Once(promise1, promise2)
				.Then(i => resolved = i);

			promise1.Resolve(5);

			Assert.Equal(5, resolved);
		}

		
		public void race_is_resolved_when_second_Promiseis_resolved_first()
		{
			var promise1 = Factory<int>(); 
			var promise2 = Factory<int>();

			var resolved = 0;

			Promise<int>
				.Once(promise1, promise2)
				.Then(i => resolved = i);

			promise2.Resolve(12);

			Assert.Equal(12, resolved);
		}

		
		public void race_is_rejected_when_first_Promiseis_rejected_first()
		{
			var promise1 = Factory<int>(); 
			var promise2 = Factory<int>();

			Exception ex = null;

			Promise<int>
				.Once(promise1, promise2)
				.Catch(e => ex = e);

			var expected = new Exception();
			promise1.Reject(expected);

			Assert.Equal(expected, ex);
		}

		
		public void race_is_rejected_when_second_Promiseis_rejected_first()
		{
			var promise1 = Factory<int>(); 
			var promise2 = Factory<int>();

			Exception ex = null;

			Promise<int>
				.Once(promise1, promise2)
				.Catch(e => ex = e);

			var expected = new Exception();
			promise2.Reject(expected);

			Assert.Equal(expected, ex);
		}

		
		public void can_resolve_Promisevia_resolver_function()
		{
			var promise = Promise<int>.Create ((resolve, reject) =>
				{
					resolve(5);
				});

			var completed = 0;
			promise.Then(v => 
				{
					Assert.Equal(5, v);
					++completed;
				});

			Assert.Equal(1, completed);
		}

		
		public void can_reject_Promisevia_reject_function()
		{
			var ex = new Exception();
			var promise = Promise<int>.Create((resolve, reject) =>
				{
					reject(ex);
				});

			var completed = 0;
			promise.Catch(e =>
				{
					Assert.Equal(ex, e);
					++completed;
				});

			Assert.Equal(1, completed);
		}

		
		public void exception_thrown_during_resolver_rejects_promise()
		{
			var ex = new Exception();
			var promise = Promise<int>.Create((resolve, reject) =>
				{
					throw ex;
				});

			var completed = 0;
			promise.Catch(e =>
				{
					Assert.Equal(ex, e);
					++completed;
				});

			Assert.Equal(1, completed);
		}        

		
		public void unhandled_exception_is_propagated_via_event()
		{
			var promise = Factory<int>();
			var ex = new Exception();
			var eventRaised = 0;

			EventHandler<ExceptionEventArgs> handler = (s, e) => 
			{
				Assert.Equal(ex, e.Exception);

				++eventRaised;
			};

			Promise.UnhandledException += handler;

			try 
			{
				promise
					.Then(a =>
						{
							throw ex;
						})
					.Done();

				promise.Resolve(5);

				Assert.Equal(1, eventRaised);
			}
			finally
			{
				Promise.UnhandledException -= handler;
			}
		}

		
		public void exception_in_done_callback_is_propagated_via_event()
		{
			var promise = Factory<int>();
			var ex = new Exception();
			var eventRaised = 0;

			EventHandler<ExceptionEventArgs> handler = (s, e) =>
			{
				Assert.Equal(ex, e.Exception);

				++eventRaised;
			};

			Promise.UnhandledException += handler;

			try
			{
				promise
					.Done(x =>
						{
							throw ex;
						});

				promise.Resolve(5);

				Assert.Equal(1, eventRaised);
			}
			finally
			{
				Promise.UnhandledException -= handler;
			}
		}

		
		public void handled_exception_is_not_propagated_via_event()
		{
			var promise = Factory<int>();
			var ex = new Exception();
			var eventRaised = 0;

			EventHandler<ExceptionEventArgs> handler = (s, e) => ++eventRaised;

			Promise.UnhandledException += handler;

			try
			{
				promise
					.Then(a =>
						{
							throw ex;
						})
					.Catch(_ => 
						{
							// Catch the error.
						})
					.Done();

				promise.Resolve(5);

				Assert.Equal(1, eventRaised);
			}
			finally
			{
				Promise.UnhandledException -= handler;
			}

		}

		
		public void can_handle_Done_onResolved()
		{
			var promise = Factory<int>();
			var callback = 0;
			var expectedValue = 5;

			promise.Done(value =>
				{
					Assert.Equal(expectedValue, value);

					++callback;
				});

			promise.Resolve(expectedValue);

			Assert.Equal(1, callback);
		}

		
		public void can_handle_Done_onResolved_with_onReject()
		{
			var promise = Factory<int>();
			var callback = 0;
			var errorCallback = 0;
			var expectedValue = 5;

			promise.Done(
				value =>
				{
					Assert.Equal(expectedValue, value);

					++callback;
				},
				ex =>
				{
					++errorCallback;
				}
			);

			promise.Resolve(expectedValue);

			Assert.Equal(1, callback);
			Assert.Equal(0, errorCallback);
		}

		public void exception_during_Then_onResolved_triggers_error_hander()
		{
			var promise = Factory<int>();
			var callback = 0;
			var errorCallback = 0;
			var expectedException = new Exception();

			promise
				.Then(value => 
					{
						throw expectedException;
					})
				.Done( (str) =>
					{
						++callback;
					},
					ex =>
					{
						Assert.Equal(expectedException, ex);

						++errorCallback;
					}
				);

			promise.Resolve(6);

			Assert.Equal(0, callback);
			Assert.Equal(1, errorCallback);
		}
	}

