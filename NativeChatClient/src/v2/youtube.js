if (Chat.info.yt) {
	// Determine the WebSocket protocol (ws:// or wss://) based on the current page protocol
	const wsProtocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';

	// Construct the WebSocket URL using the current host, a relative path, and the channel parameter
	const wsUrl = `${wsProtocol}//${window.location.host}/ws?channel=${encodeURIComponent(Chat.info.yt)}`;

	// Create the WebSocket connection
	var yt_socket = new ReconnectingWebSocket(wsUrl, null, { reconnectInterval: 5000 });

	yt_socket.onopen = function() {
		console.log('YouTube: Connected');
	};

	yt_socket.onclose = function() {
		console.log('YouTube: Disconnected');
	};

	function formatMessage(message) {
		let badges = ""
		let badge_info = true

		if (message.author.moderator == true) {
			badges += "youtubemod/1"
		}

		let info = {
			"badge-info": badge_info,
			"badges": badges,
			"color": true,
			"display-name": message.author.name,
			"emotes": true,
			"first-msg": "0",
			"flags": true,
			"id": message.id.replace(/\./g, ""),
			"mod": message.author.moderator ? 1 : 0,
			"returning-chatter": "0",
			"room-id": "133875470",
			"subscriber": "0",
			"tmi-sent-ts": message.unix,
			"turbo": "0",
			"user-id": message.author.id,
			"user-type": true,
			"runs": message.runs
		}

		return info
	}

	yt_socket.onmessage = function(data) {
		data = JSON.parse(data.data)
		if(data.info == "deleted")
		{
			Chat.clearMessage(String(data.message))
		}
		else
		{
			let info = formatMessage(data)
			Chat.write(data.author.name, info, data.message, "youtube")
		}
	}
}