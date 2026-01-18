using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.Foundation;

namespace APKInstaller.Common
{
    /// <summary>
    /// Provides a builder for asynchronous methods that return <see cref="IAsyncAction"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>
    /// AsyncActionMethodBuilder is a value type, and thus it is copied by value.
    /// Prior to being copied, one of its Task, SetResult, or SetException members must be accessed,
    /// or else the copies may end up building distinct IAsyncAction instances.
    /// </remarks>
    /// <param name="builder">The underlying builder.</param>
    public struct AsyncActionMethodBuilder(AsyncTaskMethodBuilder builder)
    {
        /// <summary>
        /// Initializes a new <see cref="AsyncActionMethodBuilder"/>.
        /// </summary>
        /// <returns>The initialized <see cref="AsyncActionMethodBuilder"/>.</returns>
        public static AsyncActionMethodBuilder Create() => new(AsyncTaskMethodBuilder.Create());

        /// <summary>
        /// Initiates the builder's execution with the associated state machine.
        /// </summary>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine => builder.Start(ref stateMachine);

        /// <summary>
        /// Associates the builder with the state machine it represents.
        /// </summary>
        /// <param name="stateMachine">The heap-allocated state machine object.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="stateMachine"/> argument was null (<see langword="Nothing" /> in Visual Basic).</exception>
        /// <exception cref="InvalidOperationException">The builder is incorrectly initialized.</exception>
        public void SetStateMachine(IAsyncStateMachine stateMachine) => builder.SetStateMachine(stateMachine);

        /// <summary>
        /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
        /// </summary>
        /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine => builder.AwaitOnCompleted(ref awaiter, ref stateMachine);

        /// <summary>
        /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
        /// </summary>
        /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine => builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        /// <summary>
        /// Gets the <see cref="IAsyncAction"/> for this builder.
        /// </summary>
        /// <returns>The <see cref="IAsyncAction"/> representing the builder's asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">The builder is not initialized.</exception>
        public IAsyncAction Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => WindowsRuntimeSystemExtensions.AsAsyncAction(builder.Task);
        }

        /// <summary>
        /// Completes the <see cref="IAsyncAction"/> in the
        /// <see cref="TaskStatus">RanToCompletion</see> state.
        /// </summary>
        /// <exception cref="InvalidOperationException">The builder is not initialized.</exception>
        /// <exception cref="InvalidOperationException">The task has already completed.</exception>
        public void SetResult() => builder.SetResult();

        /// <summary>
        /// Completes the <see cref="IAsyncAction"/> in the
        /// <see cref="TaskStatus">Faulted</see> state with the specified exception.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to use to fault the task.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="exception"/> argument is null (<see langword="Nothing" /> in Visual Basic).</exception>
        /// <exception cref="InvalidOperationException">The builder is not initialized.</exception>
        /// <exception cref="InvalidOperationException">The task has already completed.</exception>
        public void SetException(Exception exception) => builder.SetException(exception);
    }
}
