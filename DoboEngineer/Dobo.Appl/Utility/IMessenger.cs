using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dobo.Appl.Utility;
public interface IMessenger
{
    /// <summary>
    /// Checks whether or not a given recipient has already been registered for a message.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to check for the given recipient.</typeparam>
    /// <typeparam name="TToken">The type of token to check the channel for.</typeparam>
    /// <param name="recipient">The target recipient to check the registration for.</param>
    /// <param name="token">The token used to identify the target channel to check.</param>
    /// <returns>Whether or not <paramref name="recipient"/> has already been registered for the specified message.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="recipient"/> or <paramref name="token"/> are <see langword="null"/>.</exception>
    bool IsRegistered<TMessage, TToken>(object recipient, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>;

    /// <summary>
    /// Registers a recipient for a given type of message.
    /// </summary>
    /// <typeparam name="TRecipient">The type of recipient for the message.</typeparam>
    /// <typeparam name="TMessage">The type of message to receive.</typeparam>
    /// <typeparam name="TToken">The type of token to use to pick the messages to receive.</typeparam>
    /// <param name="recipient">The recipient that will receive the messages.</param>
    /// <param name="token">A token used to determine the receiving channel to use.</param>
    /// <param name="handler">The <see cref="MessageHandler{TRecipient,TMessage}"/> to invoke when a message is received.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="recipient"/>, <paramref name="token"/> or <paramref name="handler"/> are <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when trying to register the same message twice.</exception>
    void Register<TRecipient, TMessage, TToken>(TRecipient recipient, TToken token, Action<TRecipient, TMessage> handler)
        where TRecipient : class
        where TMessage : class
        where TToken : IEquatable<TToken>;

    /// <summary>
    /// Unregisters a recipient from all registered messages.
    /// </summary>
    /// <param name="recipient">The recipient to unregister.</param>
    /// <remarks>
    /// This method will unregister the target recipient across all channels.
    /// Use this method as an easy way to lose all references to a target recipient.
    /// If the recipient has no registered handler, this method does nothing.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="recipient"/> is <see langword="null"/>.</exception>
    void UnregisterAll(object recipient);

    /// <summary>
    /// Unregisters a recipient from all messages on a specific channel.
    /// </summary>
    /// <typeparam name="TToken">The type of token to identify what channel to unregister from.</typeparam>
    /// <param name="recipient">The recipient to unregister.</param>
    /// <param name="token">The token to use to identify which handlers to unregister.</param>
    /// <remarks>If the recipient has no registered handler, this method does nothing.</remarks>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="recipient"/> or <paramref name="token"/> are <see langword="null"/>.</exception>
    void UnregisterAll<TToken>(object recipient, TToken token)
        where TToken : IEquatable<TToken>;

    /// <summary>
    /// Unregisters a recipient from messages of a given type.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to stop receiving.</typeparam>
    /// <typeparam name="TToken">The type of token to identify what channel to unregister from.</typeparam>
    /// <param name="recipient">The recipient to unregister.</param>
    /// <param name="token">The token to use to identify which handlers to unregister.</param>
    /// <remarks>If the recipient has no registered handler, this method does nothing.</remarks>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="recipient"/> or <paramref name="token"/> are <see langword="null"/>.</exception>
    void Unregister<TMessage, TToken>(object recipient, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>;

    /// <summary>
    /// Sends a message of the specified type to all registered recipients.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to send.</typeparam>
    /// <typeparam name="TToken">The type of token to identify what channel to use to send the message.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="token">The token indicating what channel to use.</param>
    /// <returns>The message that was sent (ie. <paramref name="message"/>).</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="message"/> or <paramref name="token"/> are <see langword="null"/>.</exception>
    TMessage Send<TMessage, TToken>(TMessage message, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>;

    /// <summary>
    /// Performs a cleanup on the current messenger.
    /// Invoking this method does not unregister any of the currently registered
    /// recipient, and it can be used to perform cleanup operations such as
    /// trimming the internal data structures of a messenger implementation.
    /// </summary>
    void Cleanup();

    /// <summary>
    /// Resets the <see cref="IMessenger"/> instance and unregisters all the existing recipients.
    /// </summary>
    void Reset();
}
//定义一个通用消息总线接口
public interface IMessageBus
{
    // 发布事件 (Publish-Subscribe Pattern)
    // 通常用于通知多个订阅者发生了某事，不需要返回值
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;

    // 发送命令 (Point-to-Point Pattern)
    // 通常发送给唯一的处理者
    Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default);
}
public interface IServiceComponent
{
    /// <summary>
    /// 装载服务
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    /// <param name="componentContext">组件上下文</param>
    void Load(IServiceCollection services, IDictionary<string,object> componentContext);
}
