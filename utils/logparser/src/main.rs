extern crate regex;
extern crate chrono;
extern crate getopts;
extern crate serde;
extern crate serde_json;
extern crate maxminddb;

use std::env;
use std::str::FromStr;
use std::process::Command;
use std::collections::HashMap;
use std::io::{BufReader, BufRead};
use std::fs::File;
use std::io::prelude::*;
use regex::Regex;
use chrono::{DateTime, Utc};
use getopts::Options;
use maxminddb::geoip2;

#[macro_use]
extern crate serde_derive;

const BLOCK_SINCE_DAYS : i64 = 30;
const BLOCK_STATUS_CODE : &str = "444";
const IPSET_NAME : &str = "void_cat_block";

#[derive(Serialize, Deserialize)]
struct IpStats {
	ip : String,
	hits : u64,
	country : String
}

impl IpStats {
	fn new(i : String, h : u64, mm : &mut Option<maxminddb::Reader>) -> IpStats {
		let mut c : String = String::from("XX");
		if let &mut Some(ref m) = mm {
			if let Ok(ip) = FromStr::from_str(&i) {
				if let Ok(city) = m.lookup::<geoip2::City>(ip) {
					if city.country.is_some() {
						c = city.country.unwrap().iso_code.unwrap().to_lowercase();
					}
				}
			}
		}
		
		IpStats { ip: i, hits: h, country: c }
	}
}

fn gen_report(hm : HashMap<String, IpStats>, output : String) {
	//convert hashmap into vector for sorting
	let mut ordered_stats = Vec::new();
	ordered_stats.extend(hm.values());
	
	ordered_stats.sort_by(|a, b| b.hits.cmp(&a.hits));

	if let Ok(json) = serde_json::to_string(&ordered_stats) {
		//send email report
		println!("Saving report..");
		if let Ok(mut fout) = File::create(&output) {
			match fout.write_all(json.as_bytes()) {
				Ok(_) => println!("Report saved to: {}", &output),
				Err(e) => println!("Report save failed: {}", e)
			}
		}
	}
}

fn main() {
	let args: Vec<String> = env::args().collect();

    let mut opts = Options::new();
	opts.optopt("f", "file", "Log file to read", "");
	opts.optopt("d", "db", "MaxMind DB", "");
	opts.optopt("o", "out", "Output report path", "");
	opts.optflag("v", "verbose", "Print more info");
	
    let flags = match opts.parse(&args[1..]) {
        Ok(m) => { m }
        Err(f) => { panic!(f.to_string()) }
    };
	
	let file_name = match flags.opt_str("f"){
		Some(x) => x,
		None => String::from("access.log")
	};
	
	let re = Regex::new(r###"(?P<IP>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}) - - \[(?P<Date>.*)\] "(?P<Method>[A-Z]{1,5}) (?P<Path>.*) (?P<Version>HTTP/\d{1}\.[0-9]{1})" (?P<ResponseCode>\d{1,4}) (?P<ResponseLength>\d{1,11}) "(?P<Referer>.*)" "(?P<UserAgent>.*)""###).unwrap();
	if let Ok(f) = File::open(&file_name) {
		println!("Reading file: {}", &file_name);
		let file = BufReader::new(&f);
		
		let mut block_list = HashMap::<String, IpStats>::new();
		let local_time : DateTime<Utc> = Utc::now();
		let mut lines = 0u64;
		let mut hit_lines = 0u64;
		
		
		//load max mind data
		let maxmind_path = match flags.opt_str("d"){
			Some(x) => x,
			None => String::from("GeoIP2-City.mmdb")
		};
		let mut mmdb = match maxminddb::Reader::open(&maxmind_path) {
			Ok(m) => {
				println!("MaxMind DB loaded!");
				Some(m)
			},
			Err(e) => {
				println!("Could not load MaxMind DB: {}", e);
				None
			}
		};
	
		for (_, line) in file.lines().enumerate() {
			if let Ok(l) = line {
				lines += 1;
				if l.contains(BLOCK_STATUS_CODE) { //simple check if the line contains the error code
					hit_lines += 1;
					if let Some(tokens) = re.captures(&l) {
						if let (Some(ip), Some(date), Some(rsp)) = (tokens.get(1), tokens.get(2), tokens.get(6)) {
							//println!("Got tokens: {} {} {}", ip.as_str(), date.as_str(), rsp.as_str());
							if let Ok(dt) = DateTime::parse_from_str(date.as_str(), "%d/%b/%Y:%H:%M:%S %z") {
								let dt_utc = dt.with_timezone(&Utc);								
								let slog = local_time.signed_duration_since(dt_utc);

								//println!("Row match: {}", rsp.as_str() == BLOCK_STATUS_CODE);
								if rsp.as_str() == BLOCK_STATUS_CODE && slog.num_days() <= BLOCK_SINCE_DAYS {
									let mut host_stat = block_list.entry(ip.as_str().to_owned()).or_insert(IpStats::new(ip.as_str().to_owned(), 0u64, &mut mmdb));
									host_stat.hits += 1;
								}
							}
						}
					}
				}
			}
		}
		let verbose = flags.opt_present("v");
				
		println!("Blocking {} ips, from {}/{} {:.2}% requests", block_list.len(), hit_lines, lines, 100f64 * (hit_lines as f64 / lines as f64));
		match Command::new("/sbin/ipset").args(&["flush", &IPSET_NAME]).output() {
			Ok(_) => {
				println!("[OK]\t:ipset flush");
				for (k,_) in &block_list {
					match Command::new("/sbin/ipset").args(&["add", &IPSET_NAME, &k]).output() {
						Ok(_) => {
							if verbose { 
								println!("[OK]\t:ipset add {} {}", &IPSET_NAME, &k);
							}
						},
						Err(msg) => println!("[F]\t{} ({})", k, msg)
					}
				}
			},
			Err(msg) => println!("Failed to run ipset flush command: {}", msg)
		}
		
		let output = match flags.opt_str("o") {
			Some(x) => x,
			None => String::from("report.html")
		};
		//send email report
		gen_report(block_list, output);
	}else{
		println!("File not found: {}", &file_name);
	}
}
