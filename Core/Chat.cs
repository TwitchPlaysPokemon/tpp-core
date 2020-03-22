using System;
using Core.Configuration;
using Inputting.Parsing;
using Persistence.Repos;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace Core
{
	public class Chat
	{
		/// <summary>
		/// Delegate for handling an incoming <see cref="Message"/>.
		/// </summary>
		public delegate void OnMessageEventHandler(object sender, Message e);
		public event OnMessageEventHandler OnMessage = null!;

		private TwitchClient _client = null!;

		private readonly IrcConfig _ircConfig;
		private readonly IUserRepo _userRepo;
		private readonly Func<IInputParser> _inputParserProvider;

		public Chat(IrcConfig ircConfig, IUserRepo userRepo, Func<IInputParser> inputParserProvider)
		{
			_ircConfig = ircConfig;
			_userRepo = userRepo;
			_inputParserProvider = inputParserProvider;
		}

		public void Start()
		{
			var credentials = new ConnectionCredentials(
				twitchUsername: _ircConfig.Username,
				twitchOAuth: _ircConfig.Password);

			var options = new ClientOptions();
			_client = new TwitchClient(new TcpClient(options));
			_client.Initialize(credentials);

			_client.OnMessageReceived += ClientOnMessageReceived;

			_client.Connect();
			_client.JoinChannel(_ircConfig.Channel);
		}

		private async void ClientOnMessageReceived(object? sender, OnMessageReceivedArgs e)
		{
			var chatMessage = e.ChatMessage;
			var user = await _userRepo.RecordUser(new UserInfo(
				id: chatMessage.UserId,
				twitchDisplayName: chatMessage.DisplayName,
				simpleName: chatMessage.Username,
				color: chatMessage.ColorHex == string.Empty ? null : chatMessage.ColorHex,
				fromMessage: true
			));
			string firstArgument = chatMessage.Message.Split(" ", count: 2)[0];
			var inputSequence = _inputParserProvider().Parse(firstArgument);
			OnMessage?.Invoke(this, new Message(user, chatMessage.Message, inputSequence));
		}

		public void Stop()
		{
			_client.Disconnect();
		}
	}
}
