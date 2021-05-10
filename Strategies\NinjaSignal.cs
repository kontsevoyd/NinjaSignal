#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using System.Net;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
	public class NinjaSignal : Strategy
	{

		private double last_order = 0;
		private long last_order_id = 0;
		
		private long last_order_close_id = 0;
		
		private long canceled_order_id = 0;

		private Dictionary<string, double> last_position = new Dictionary<string, double>();
		
		private Dictionary<string, double> last_order_close = new Dictionary<string, double>();
		
		private Dictionary<string, double> positions_count = new Dictionary<string, double>();
		
		private List<string> message = new List<string>();
		
		private List<List<string>> messages = new List<List<string>>();
		
		private bool allow_reload = true;
		
		public bool sender = true;
		
		protected override void OnStateChange()
		{
			

			if (State == State.SetDefaults)
    		{
        		Calculate = Calculate.OnPriceChange;
   			}
			else{
				last_order = 0;
				positions_count.Clear();
				
				last_position.Clear();
				
				last_order_close.Clear();
				
				messages.Clear();
				
				allow_reload = true;
				
				sender = true;
			}
		}
		
		
		
		public void SendMessage(List<string> list){
			
			sender = false;
						
			
			string bot_token = "<bot token>";
			string chat_id = "<chat id>";
			
			
			string url = "https://api.telegram.org/bot"+bot_token+"/sendMessage?chat_id="+chat_id+"&parse_mode=html&text=";			
			string msg = "";
		
		
			if(list[0] == "close_position"){
				if(list[3] == "Long"){
					msg = "Закрыли шорт: "+list[1].Replace(" Globex", "").Replace(" Nymex", "")+"%0AЦена: "+Math.Round(Double.Parse(list[2]), 2);
				}
				else{
					msg = "Закрыли лонг: "+list[1].Replace(" Globex", "").Replace(" Nymex", "")+"%0AЦена: "+Math.Round(Double.Parse(list[2]), 2);
				}
			}
			else if(list[0] == "open_position"){
				if(list[1] == "Long"){
					msg = "Лонг: "+list[2].Replace(" Globex", "").Replace(" Nymex", "")+"%0AЦена: "+Math.Round(Double.Parse(list[3]), 2);
				}
				else{
					msg = "Шорт: "+list[2].Replace(" Globex", "").Replace(" Nymex", "")+"%0AЦена: "+Math.Round(Double.Parse(list[3]), 2);
				}
			}
			else if(list[0] == "limit_buy"){
				msg = "Покупка%20"+list[1]+"%20по%20"+list[2];		
			}
			else if(list[0] == "stop_buy"){
				if(list[4] == "0"){
					msg = ""+list[1]+"%20"+list[2]+"%20стоп%20"+list[3];
				}
				else{
					msg = ""+list[1]+"%20"+list[2]+"%20стоп%20"+list[3]+"%20лимит%20"+list[4];
				}
			}
			else if(list[0] == "order_cancel"){
				msg = "Ордер%20отменен%20"+list[1]+"%20по%20"+list[2];					
			}
			else if(list[0] == "stop_loss_buy"){
				msg = "Стоп лосс: "+list[2].Replace(" Globex", "").Replace(" Nymex", "")+"%0AЦена: "+Math.Round(Double.Parse(list[3]), 2);
			}
			
			url = url + msg;
			
			using (var wb = new WebClient()){
			    			
				try
	    		{
					var response = wb.DownloadString(url);
					sender = true;
				}
				catch (Exception ex)
	    		{
					return;
	    		}
				
			}
			
		}
		
		
		
		protected override void OnBarUpdate()
		{
			
			Dictionary<string, double> temp_count = new Dictionary<string, double>();
			
			temp_count = positions_count;
			
			foreach(Position pos in Account.Positions){
				temp_count.Remove(pos.Instrument.ToString());
			}
			
			
			foreach(string name in temp_count.Keys){
				if(!last_position.ContainsKey(name)){
					last_position.Add(name, 0);
				}

				if(last_position[name] != 0){

					message.Add("close_position");	
					message.Add(name);
					for(int i = Account.Orders.Count - 1; i >= 0; i--){
						if(Account.Orders.ElementAt(i).Instrument.ToString() == name){
							message.Add(Account.Orders.ElementAt(i).AverageFillPrice.ToString());
							if(Account.Orders.ElementAt(i).IsLong){
								message.Add("Long");
							}
							else{
								message.Add("Short");
							}
							
							break;
						}
					}
					
					SendMessage(message);
					message.Clear();
					
					last_position[name] = 0;
				}

			}
			
			positions_count.Clear();
			
			foreach(Position pos in Account.Positions){

				if(!positions_count.ContainsKey(pos.Instrument.ToString())){
					positions_count.Add(pos.Instrument.ToString(), 1);
				}

			}
			
			var temp_p = Account.Positions;
			
			foreach(Position pos in temp_p){
				
				if(!last_position.ContainsKey(pos.Instrument.ToString())){
					last_position.Add(pos.Instrument.ToString(), 0);
				}
				
				if(last_position[pos.Instrument.ToString()] != pos.AveragePrice){
					
					message.Add("open_position");
					message.Add(pos.MarketPosition.ToString());
					message.Add(pos.Instrument.ToString());
					message.Add(pos.AveragePrice.ToString());
					SendMessage(message);
					//messages.Add(message);
					
					message.Clear();

					last_position[pos.Instrument.ToString()] = pos.AveragePrice;

					return;
				}	

			}
						

		}
	}
}
