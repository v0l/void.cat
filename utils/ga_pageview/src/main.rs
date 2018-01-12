extern crate redis;

use std::process::Command;

fn main() {
	let ch = "ga-page-view";
	let mut q = Vec::new();

    if let Ok(client) = redis::Client::open("redis://127.0.0.1/") {
		if let Ok(mut pubsub) = client.get_pubsub() {
			if let Ok(_) = pubsub.subscribe(ch) {
				println!("Subscribed to {}", ch);
				
				loop {
					if let Ok(msg) = pubsub.get_message() {
						if let Ok(payload) = msg.get_payload::<String>() {
							//println!("channel '{}': {}", msg.get_channel_name(), payload);
							
							if q.len() >= 20 {
								//push the rows to ga /batch
								Command::new("curl").arg("-X").arg("POST").arg("--data").arg(q.join(" \\\r\n")).arg("https://www.google-analytics.com/batch").output().expect("failed to execute process");
								q.clear();
							}
							q.push(payload);
						}
					}
				}
			}else {
				println!("Failed to subscribe");
			}
		}else {
			println!("Failed to get pubsub");
		}
	}else {
		println!("Failed to connect to redis");
	}
}
