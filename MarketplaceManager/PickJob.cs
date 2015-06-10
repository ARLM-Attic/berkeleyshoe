﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerkeleyEntities;
using System.Data.Entity;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;

namespace MarketplaceManager
{
    public class PickJob
    {
        private berkeleyEntities _dataContext = new berkeleyEntities();

        public string Code { get; set; }

        public List<PendingOrder> PendingOrders { get; set; }

        public List<PendingOrder> CompletedOrders { get; set; }

        public void FetchUnshippedOrders()
        {
            var amznUnshippedOrders = _dataContext.AmznOrders
                .Where(p => p.Status.Equals("Unshipped") || p.Status.Equals("PartiallyShipped")).ToList()
                .Where(p => p.OrderItems.All(s => s.ListingItem != null && s.ListingItem.Item != null)).ToList();

            foreach (var order in amznUnshippedOrders)
            {
                AmznMarketplace marketplace = _dataContext.AmznMarketplaces.Single(p => p.ID == order.MarketplaceID);

                PendingOrder pendingOrder = new PendingOrder();
                pendingOrder.OrderID = order.Code;
                pendingOrder.MarketplaceCode = marketplace.Code;
                pendingOrder.MarketplaceName = marketplace.Name;
                pendingOrder.PrintTime = order.PrintTime;
                pendingOrder.AddressLine1 = order.AddressLine1;
                pendingOrder.AddressLine2 = order.AddressLine2;
                pendingOrder.AddressLine3 = order.AddressLine3;
                pendingOrder.BuyerName = order.BuyerName;
                pendingOrder.City = order.City;
                pendingOrder.StateOrRegion = order.StateOrRegion;
                pendingOrder.PurchaseDate = order.PurchaseDate;
                pendingOrder.PostalCode = order.PostalCode;
                pendingOrder.Phone = order.Phone;
                pendingOrder.Country = order.CountryCode;
                pendingOrder.ShippingPrice = order.OrderItems.Sum(p => p.ShippingPrice - p.ShippingDiscount);
             
                foreach (var orderItem in order.OrderItems)
                {
                    PendingOrderItem pendingOrderItem = new PendingOrderItem();
                    pendingOrderItem.Sku = orderItem.ListingItem.Item.ItemLookupCode;
                    pendingOrderItem.BinLocation = orderItem.ListingItem.Item.BinLocation;
                    pendingOrderItem.Qty = orderItem.QuantityOrdered;
                    pendingOrderItem.OnHand = (int)orderItem.ListingItem.Item.Quantity;
                    pendingOrderItem.Price = orderItem.ItemPrice / orderItem.QuantityOrdered;
                    pendingOrderItem.Discount = orderItem.PromotionDiscount;
                    pendingOrder.Items.Add(pendingOrderItem);
                }

                this.PendingOrders.Add(pendingOrder);
            }
                
            var ebayUnshippedOrders = _dataContext.EbayOrders.ToList()
                .Where(p => p.IsWaitingForShipment()).ToList()
                .Where(p => p.OrderItems.All(s => s.ListingItem != null && s.ListingItem.Item != null)).ToList();

            foreach (var order in ebayUnshippedOrders)
            {
                EbayMarketplace marketplace = _dataContext.EbayMarketplaces.Single(p => p.ID == order.MarketplaceID);

                PendingOrder pendingOrder = new PendingOrder();
                pendingOrder.OrderID = order.Code;
                pendingOrder.MarketplaceCode = marketplace.Code;
                pendingOrder.MarketplaceName = marketplace.Name;
                pendingOrder.PrintTime = order.PrintTime;
                pendingOrder.AddressLine1 = order.Street1;
                pendingOrder.AddressLine2 = order.Street2;
                pendingOrder.BuyerName = order.UserName;
                pendingOrder.City = order.CityName;
                pendingOrder.StateOrRegion = order.StateOrProvince;
                pendingOrder.PurchaseDate = order.CreatedTime;
                pendingOrder.PostalCode = order.PostalCode;
                pendingOrder.Phone = order.Phone;
                pendingOrder.Country = order.CountryCode;
                pendingOrder.ShippingPrice = order.ShippingServiceCost;
                
                foreach (var orderItem in order.OrderItems)
                {
                    PendingOrderItem pendingOrderItem = new PendingOrderItem();
                    pendingOrderItem.Sku = orderItem.ListingItem.Item.ItemLookupCode;
                    pendingOrderItem.BinLocation = orderItem.ListingItem.Item.BinLocation;
                    pendingOrderItem.Qty = orderItem.QuantityPurchased;
                    pendingOrderItem.OnHand = (int)orderItem.ListingItem.Item.Quantity;
                    pendingOrderItem.Price = orderItem.TransactionPrice * orderItem.QuantityPurchased;
                    
                    pendingOrder.Items.Add(pendingOrderItem);
                }

                this.PendingOrders.Add(pendingOrder);
            }
        }

        private void GeneratePrintFile(string file)
        {
            StreamWriter stream = File.CreateText(file);

            stream.AutoFlush = true;

            stream.Write("<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'>" +
                "<html xmlns='http://www.w3.org/1999/xhtml'><head><meta http-equiv='Content-Type' content='text/html; charset=utf-8' /><title>" +
                "Untitled Document</title><style>body {font-family:Tahoma, Geneva, sans-serif;font-size:10px;} p{margin:0px;margin-bottom:5px;} .customerName {font-family:Arial,Helvetica, sans-serif;font-size:large;font-weight:bold;} .titulos {font-family:Arial," +
                "Helvetica, sans-serif;font-size:large;font-weight:bold;" +
                //((gCurrentMarketplace % 2 == 0) ? "border-style:solid; border-width:1px;" : " ") +
                "}.fineprint {font-family:Arial, Helvetica, sans-serif;font-size:x-small;" +
                "font-weight: normal;text-align:center;} .repeated-order {border-width: 2px;border-color: black;border-style:solid;padding: 2px; margin-right:2px;font-family:Arial, Helvetica, sans-serif;font-size:15px;float:right;} .day-of-week {padding: 1px;background-color: black;color: white;}</style></head><body><div style='width: 800px'>");


            int index = 0;

            foreach (PendingOrder order in this.PendingOrders.OrderBy(p => p.Items.First().BinLocation))
            {
                index++;

                stream.Write("<table border='0' bordercolor='#000000' cellpadding='1' cellspacing='0' width='100%'><tr><td style='height:5in'><center>");
                stream.Write("<table width='800' border='0' cellspacing='0' cellpadding='0'><tr><td align='left' valign='top' width='33%'>");

                stream.Write("<tr>" +
                        "<td align='left' valign='top' width='33%'><p class='titulos'>" + order.BuyerName + "</p><p><strong>" +
                        order.AddressLine1 + "<br />" +
                        order.AddressLine2 + "<br />" +
                        order.AddressLine3 + "<br />" +
                        order.City + ", " + order.StateOrRegion + " " + order.PostalCode + "<br />" +
                        order.Country + "<br />" +
                        order.Phone + "</strong></p></td>" +
                        "    <td align='center' valign='top' width='34%'><p class='titulos'><u>" + order.MarketplaceName + "</u></p>" +
                        "    <p><strong>THANK YOU FOR YOUR ORDER</strong></p>" + GetRepeatedInfo(order.PrintTime) + "</td>" +
                        "    <td align='right' valign='top' width='33%'><p>" + order.PurchaseDate.ToShortDateString() + " - " + index + "</p>" +
                        "    <p><h2>" + order.OrderID + "</h2></p>" +
                        "    <p><font face='Free 3 of 9 Extended' size='+2'>*" + order.OrderID + "*</font></p>" +
                        "    <p><h2>" + GetShippingInfo(order) + "</h2></p></td>" +
                        "  </tr>");

                stream.Write("</table></center>");

                stream.Write("<table width='800' border='1' cellspacing='0' cellpadding='2'><tr><td width='7%'><div align='center'><b>Qty</b></font></div></td><td width='58%'><div align='center'><font><b>Product</b></font></div></td><td width='21%'><div align='center'><font><b>Price</b></font></div></td><td width='14%'><div align='center'><font><b>Total</b></font></div></td></tr>");

                decimal subTotal = 0;

                foreach (PendingOrderItem orderItem in order.Items)
                {
                    stream.Write("  <tr>" +
                                "    <td><div align='center'>" + orderItem.Qty + "</div></td>" +
                                "    <td><div align='left'><b>" + orderItem.Title + "</b><br />" +
                                "        <strong>SKU: " + orderItem.Sku + " | " + orderItem.BinLocation + "</strong></div></td>" +
                                "    <td><div align='right'>" + (orderItem.Price - orderItem.Discount).ToString("C") + "</div></td>" +
                                "    <td><div align='right'>" + subTotal.ToString("C") + "</div></td>" +
                                "  </tr>");
                }

                stream.Write("<tr><td><div align='center'></div></td><td><div align='left'></div></td>" +
                            "<td><div align='right'>Shipping:</div></td><td><div align='right'>" +
                            order.ShippingPrice.ToString("C") +
                            "</div></td></tr>");

                stream.Write("<tr><td><div align='center'></div></td><td><div align='left'></div></td>" +
                            "<td><div align='right'>Order total:</div></td><td><div align='right'>" +
                            order.Total.ToString("C") +
                            "</div></td></tr></table>");

                stream.Write("<table width='100%' align='center' cellpadding='10' cellspacing='0' border='0'>" +
                                     "<tr><td align='left' valign='top' width='50%'><p><strong>Exchanges:</strong> " +
                                     "Looking to exchange for a different size? Contact us through Amazon messages to  " +
                                     "see if we can find the right pair for you.</p><p><strong>Returns:</strong> Our return " +
                                     "policies are stated in our profile page at <strong>Amazon.com</strong>. Please contact us through Amazon messages first. " +
                                     "We accept returns within <strong>30 days</strong> after receiving the product. " +
                                     "The items <strong>must not be worn</strong> and should be returned in their <b>original box</b> and  packaging. " +
                                     "<u>Buyer is responsible for returning shipping costs</u>.</p>" +
                                     "<br/><p align='center'><b>PLEASE CONTACT OUR CUSTOMER SERVICE</b></p>" +
                                     "<p align='center'><b>DEPARTMENT FOR RETURN INSTRUCTIONS</b></p>" +
                                     "We guarantee a reply to your return request within 24hrs. If you do not receive anything, we recommend checking your spam/junk mail." +
                                     "</td><td align='left' valign='top' width='50%'>" +
                                     "<p>To better serve our customers in the future, please write down the reason for returning the item:</p>" +
                                     "<p>&nbsp;</p><p>&nbsp;</p><p>&nbsp;</p><p>&nbsp;</p><p>&nbsp;</p><p><strong>Once we receive the shoes we will fully refund your  order. " +
                                     "Inspection and processing usually take </strong><strong>3-5 business days</strong><strong>.</strong><br /><strong>" +
                                     "We don't refund original  shipping charges.</strong></p></td></tr></table><p class='fineprint'>" +
                                     "*You MUST INCLUDE THIS PAGE in your return; otherwise we will have trouble  processing your order. " +
                                     "Contact us through Amazon messages for any questions.</p><!--p align='center'><strong>VISIT OUR WEBSITE " +
                                     "<a href='http://WWW.MECALZO.COM'>WWW.MECALZO.COM</a>  USE CODE &ldquo;RETCUST&rdquo;  " +
                                     "AT CHECKOUT AND GET $5.OO OFF THE TOTAL OF YOUR ORDER</strong></p-->");

                stream.Write("</td></tr></table>");

                if (index % 2 == 0)
                {
                    stream.Write("<p style='page-break-before: always;'>&nbsp;</p>");
                }
                else
                {
                    stream.Write("<hr style='margin:2px;padding:0px;' />");
                }

                stream.Flush();
            }

            //var printOrders = orders.Where(p => p.PrintTime.HasValue == false);

            //string POBoxPattern = @"(?i)\b(?:p(?:ost)?\.?\s*[o0](?:ffice)?\.?\s*b(?:[o0]x)?|b[o0]x)";

            //var poBoxOrders = printOrders.Where(p => Regex.IsMatch(p.AddressLine1 ?? "", POBoxPattern) || Regex.IsMatch(p.AddressLine2 ?? "", POBoxPattern) || Regex.IsMatch(p.AddressLine3 ?? "", POBoxPattern));

            //stream.Write("<h1>eBay " + view.Name + " orders summary " + DateTime.Now.ToShortDateString() + "</h1><hr>" +
            //            "<p>&nbsp;<b>TOTAL ORDERS       :" + printOrders.Count() + "</b></p>" +
            //            "<p>&nbsp;<b>TOTAL NEXT DAY     :" + printOrders.Where(p => p.ShipmentServiceLevelCategory != null).Where(p => p.ShipmentServiceLevelCategory.Contains("Expedited") || p.ShipmentServiceLevelCategory.Contains("NextDay") || p.ShipmentServiceLevelCategory.Contains("SameDay")).Count() + "</b></p>" +
            //            "<p>&nbsp;<b>TOTAL PO BOX       :" + poBoxOrders.Count() + "</b></p>" +
            //            "<p>&nbsp;<b>TOTAL INTERNATIONAL:" + printOrders.Where(p => !p.CountryCode.Contains("US")).Count() + "</b></p>" +
            //            "<p>&nbsp;<b>TOTAL ITEMS        :" + printOrders.Sum(p => p.OrderItems.Sum(s => s.QuantityOrdered)) + "</b></p>" +
            //            "<p>&nbsp;<b>TOTAL SHIPPING     :" + printOrders.Sum(p => p.OrderItems.Sum(s => s.ShippingPrice - s.ShippingDiscount)).ToString("C") + "</b></p>" +
            //            "<p>&nbsp;<b>GRAND TOTAL        :" + printOrders.Sum(p => p.Total).ToString("C") + "</b></p>" +
            //            "<p>&nbsp;</p><p>&nbsp;</p><p>DETAILS</p><ul>");


            //var brands = printOrders.SelectMany(p => p.OrderItems).GroupBy(p => p.ListingItem.Item.SubDescription1).OrderBy(p => p.Key);

            //foreach (var brand in brands)
            //{
            //    stream.Write("<li>" + brand.Key + " (" + brand.Sum(s => s.QuantityOrdered) + ") " + string.Format("{0:C}", brand.Sum(s => s.ItemPrice)) + "</li>");
            //};

            stream.Write("</ul></div></body></html>");

            stream.Flush();

            stream.Close();
        }

        private string GetShippingInfo(PendingOrder order)
        {
            string shippingInfo = "N/A";
            
            if (order.Country.Equals("US"))
            {
                string zone = DetermineZone(order.PostalCode);

                string POBoxPattern = @"(?i)\b(?:p(?:ost)?\.?\s*[o0](?:ffice)?\.?\s*b(?:[o0]x)?|b[o0]x)";

                if ((order.ShippingService.Contains("Expedited") || order.ShippingService.Contains("NextDay") || order.ShippingService.Contains("SameDay") || order.ShippingService.Equals("Overnight")))
                {
                    shippingInfo = "NEXT DAY | UPS";
                }
                else if (Regex.IsMatch(order.AddressLine1 ?? "", POBoxPattern) || Regex.IsMatch(order.AddressLine2 ?? "", POBoxPattern) || Regex.IsMatch(order.AddressLine3 ?? "", POBoxPattern))
                {
                    shippingInfo = "USPS";
                }
                else
                {
                    switch (zone)
                    {
                        case "1":
                        case "2":
                        case "3":
                        case "4":
                            shippingInfo = "USPS"; break;
                        default:
                            shippingInfo = string.Format("Zone: {0}", zone); break;

                    }
                }
            }
            else
            {
                shippingInfo = "USPS";
            }

            return shippingInfo;
        }

        private string GetRepeatedInfo(DateTime? printTime)
        {
            if (printTime.HasValue)
            {
                string dayOfWeek = printTime.Value.DayOfWeek.ToString().Substring(0, 3).ToUpper();
                string shortDate = printTime.Value.ToShortDateString();

                return "<p class='repeated-order'><span class='day-of-week'>" + dayOfWeek + "</span>&nbsp;" + shortDate + "&nbsp;</p>";
            }
            else
            {
                return string.Empty;
            }
        }
      
        private string DetermineZone(string postalCode)
        {
            #region
            String[] zipCodes = { 
              "005","006","007","008","009","010","011","012","013","014","015","016","017","018","019","020","021","022","023","024","025",
              "026","027","028","029","030","031","032","033","034","035","036","037","038","039","040","041","042","043","044","045","046",
              "047","048","049","050","051","052","053","054","055","056","057","058","059","060","061","062","063","064","065","066","067",
              "068","069","070","071","072","073","074","075","076","077","078","079","080","081","082","083","084","085","086","087","088",
              "089","090","091","092","093","094","095","096","097","098","099","100","101","102","103","104","105","106","107","108","109",
              "110","111","112","113","114","115","116","117","118","119","120","121","122","123","124","125","126","127","128","129","130",
              "131","132","133","134","135","136","137","138","139","140","141","142","143","144","145","146","147","148","149","150","151",
              "152","153","154","155","156","157","158","159","160","161","162","163","164","165","166","167","168","169","170","171","172",
              "173","174","175","176","177","178","179","180","181","182","183","184","185","186","187","188","189","190","191","192","193",
              "194","195","196","197","198","199","200","201","202","203","204","205","206","207","208","209","210","211","212","213","214",
              "215","216","217","218","219","220","221","222","223","224","225","226","227","228","229","230","231","232","233","234","235",
              "236","237","238","239","240","241","242","243","244","245","246","247","248","249","250","251","252","253","254","255","256",
              "257","258","259","260","261","262","263","264","265","266","267","268","269","270","271","272","273","274","275","276","277",
              "278","279","280","281","282","283","284","285","286","287","288","289","290","291","292","293","294","295","296","297","298",
              "299","300","301","302","303","304","305","306","307","308","309","310","311","312","313","314","315","316","317","318","319",
              "320","321","322","323","324","325","326","327","328","329","330","331","332","333","334","335","336","337","338","339","340",
              "341","342","343","344","345","346","347","348","349","350","351","352","353","354","355","356","357","358","359","360","361",
              "362","363","364","365","366","367","368","369","370","371","372","373","374","375","376","377","378","379","380","381","382",
              "383","384","385","386","387","388","389","390","391","392","393","394","395","396","397","398","399","400","401","402","403",
              "404","405","406","407","408","409","410","411","412","413","414","415","416","417","418","419","420","421","422","423","424",
              "425","426","427","428","429","430","431","432","433","434","435","436","437","438","439","440","441","442","443","444","445",
              "446","447","448","449","450","451","452","453","454","455","456","457","458","459","460","461","462","463","464","465","466",
              "467","468","469","470","471","472","473","474","475","476","477","478","479","480","481","482","483","484","485","486","487",
              "488","489","490","491","492","493","494","495","496","497","498","499","500","501","502","503","504","505","506","507","508",
              "509","510","511","512","513","514","515","516","517","518","519","520","521","522","523","524","525","526","527","528","529",
              "530","531","532","533","534","535","536","537","538","539","540","541","542","543","544","545","546","547","548","549","550",
              "551","552","553","554","555","556","557","558","559","560","561","562","563","564","565","566","567","568","569","570","571",
              "572","573","574","575","576","577","578","579","580","581","582","583","584","585","586","587","588","589","590","591","592",
              "593","594","595","596","597","598","599","600","601","602","603","604","605","606","607","608","609","610","611","612","613",
              "614","615","616","617","618","619","620","621","622","623","624","625","626","627","628","629","630","631","632","633","634",
              "635","636","637","638","639","640","641","642","643","644","645","646","647","648","649","650","651","652","653","654","655",
              "656","657","658","659","660","661","662","663","664","665","666","667","668","669","670","671","672","673","674","675","676",
              "677","678","679","680","681","682","683","684","685","686","687","688","689","690","691","692","693","694","695","696","697",
              "698","699","700","701","702","703","704","705","706","707","708","709","710","711","712","713","714","715","716","717","718",
              "719","720","721","722","723","724","725","726","727","728","729","730","731","732","733","734","735","736","737","738","739",
              "740","741","742","743","744","745","746","747","748","749","750","751","752","753","754","755","756","757","758","759","760",
              "761","762","763","764","765","766","767","768","769","770","771","772","773","774","775","776","777","778","779","780","781",
              "782","783","784","785","786","787","788","789","790","791","792","793","794","795","796","797","798","799","800","801","802",
              "803","804","805","806","807","808","809","810","811","812","813","814","815","816","817","818","819","820","821","822","823",
              "824","825","826","827","828","829","830","831","832","833","834","835","836","837","838","839","840","841","842","843","844",
              "845","846","847","848","849","850","851","852","853","854","855","856","857","858","859","860","861","862","863","864","865",
              "866","867","868","869","870","871","872","873","874","875","876","877","878","879","880","881","882","883","884","885","886",
              "887","888","889","890","891","892","893","894","895","896","897","898","899","900","901","902","903","904","905","906","907",
              "908","909","910","911","912","913","914","915","916","917","918","919","920","921","922","923","924","925","926","927","928",
              "929","930","931","932","933","934","935","936","937","938","939","940","941","942","943","944","945","946","947","948","949",
              "950","951","952","953","954","955","956","957","958","959","960","961","962","963","964","965","966","967","968","969","970",
              "971","972","973","974","975","976","977","978","979","980","981","982","983","984","985","986","987","988","989","990","991",
              "992","993","994","995","996","997","998","999" };

            String[] zones = { 
                "2","7","7","7","7","2","2","2","2","1","1","1","1","1","1","1","1","1","1","1","1","1","1","1",
                "1","1","1","1","1","1","2","2","2","1","2","2","2","2","2","3","2","3","3","2","3","2","2","2",
                "2","2","1","2","2","2","2","2","2","2","2","2","2","2","2","2","2","3","3","3","3","3","3","3",
                "3","3","3","3","3","3","3","3","3","3","3","3","3","3","3","3","3","3","3","3","3","3","3","3",
                "3","3","3","3","2","2","2","2","2","3","3","3","3","3","2","3","2","2","2","2","2","2","2","2",
                "2","2","2","2","3","3","3","3","3","3","3","3","3","3","3","4","4","4","4","4","4","4","4","4",
                "4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","3","3","3",
                "3","3","3","3","4","3","3","3","3","3","3","3","3","3","3","3","3","3","3","3","3","3","3","3",
                "3","3","3","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4",
                "4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","5","4","4",
                "4","5","5","5","5","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4","4",
                "4","4","4","4","4","4","4","4","4","4","4","5","5","5","5","5","4","4","5","5","5","5","5","5",
                "5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","6",
                "5","5","5","5","6","5","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6",
                "6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","5","5","5","6","6","6","5","6","6",
                "6","6","6","6","6","5","5","5","5","5","6","5","5","5","5","6","6","6","6","5","5","6","6","6",
                "6","6","6","6","6","6","6","6","6","5","5","5","5","5","5","5","5","5","5","5","5","5","4","4",
                "5","5","4","4","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5",
                "5","5","4","4","4","4","4","4","4","4","4","4","4","5","5","5","5","5","5","5","4","5","5","5",
                "5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","5",
                "5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","6","6","6","6","6","6","6","6","6",
                "6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","5","5","5","5","5","5","5",
                "5","5","5","5","5","5","5","6","5","5","5","5","5","5","5","5","5","6","6","6","6","6","6","6",
                "6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","7","7","7","7","7","6",
                "6","6","6","6","7","7","7","7","7","8","8","7","8","8","8","8","8","8","8","5","5","5","5","5",
                "5","5","5","5","5","5","5","5","5","5","5","5","5","5","5","6","6","6","6","5","5","5","5","5",
                "5","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6",
                "6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","7","7",
                "7","7","7","6","6","6","6","6","6","6","6","6","6","7","7","7","7","7","7","7","7","7","7","6",
                "6","6","6","6","7","7","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6","6",
                "6","6","6","6","6","7","7","7","7","7","7","7","7","7","7","6","6","6","6","6","6","7","6","7",
                "6","7","7","7","7","7","6","7","7","7","7","7","7","7","7","7","7","7","7","7","8","7","7","7",
                "7","7","7","7","7","7","7","7","7","7","7","7","8","7","7","7","7","7","7","7","7","7","7","7",
                "8","8","8","7","7","7","7","7","7","7","7","7","7","7","8","8","8","8","8","8","8","8","8","7",
                "8","7","8","7","7","7","7","7","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8",
                "8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8",
                "8","8","8","8","8","8","8","8","8","8","8","8","7","8","8","7","8","8","8","8","8","8","8","8",
                "8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8",
                "8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8",
                "8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8",
                "8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8","8",
                "8","8","8","8","8","8","8","8","8","8","8"
                };
            #endregion

            string zone = "NF";

            int i = 0;

            string prefix = postalCode.Substring(0, 3);

            foreach (string zip in zipCodes)
            {
                if (zip.Contains(prefix))
                {
                    zone = zones[i];
                    break;
                }
                i++;
            }

            return zone;
        }

        //private void GeneratePrintFile(List<PendingOrder> orders, string file)
        //{
        //    StreamWriter stream = File.CreateText(file);

        //    stream.AutoFlush = true;

        //    stream.Write("<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'>" +
        //        "<html xmlns='http://www.w3.org/1999/xhtml'><head><meta http-equiv='Content-Type' content='text/html; charset=utf-8' /><title>" +
        //        "Untitled Document</title><style>body {font-family:Tahoma, Geneva, sans-serif;font-size:10px;} p{margin:0px;margin-bottom:5px;} .customerName {font-family:Arial,Helvetica, sans-serif;font-size:large;font-weight:bold;} .titulos {font-family:Arial," +
        //        "Helvetica, sans-serif;font-size:large;font-weight:bold;" +
        //        //((gCurrentMarketplace % 2 == 0) ? "border-style:solid; border-width:1px;" : " ") +
        //        "}.fineprint {font-family:Arial, Helvetica, sans-serif;font-size:x-small;" +
        //        "font-weight: normal;text-align:center;} .repeated-order {border-width: 2px;border-color: black;border-style:solid;padding: 2px; margin-right:2px;font-family:Arial, Helvetica, sans-serif;font-size:15px;float:right;} .day-of-week {padding: 1px;background-color: black;color: white;}</style></head><body><div style='width: 800px'>");


        //    int index = 0;

        //    foreach (PendingOrder order in orders.OrderBy(p => p.Items.First().BinLocation))
        //    {
        //        index++;

        //        stream.Write("<table border='0' bordercolor='#000000' cellpadding='1' cellspacing='0' width='100%'><tr><td style='height:5in'><center>");
        //        stream.Write("<table width='800' border='0' cellspacing='0' cellpadding='0'><tr><td align='left' valign='top' width='33%'>");

        //        stream.Write("<tr>" +
        //                "    <td align='left' valign='top' width='33%'><p class='customerName'>" + order.UserName + "</p><p><strong>" +
        //                order.BuyerID + "<br />" +
        //                order.CompanyName + "<br />" +
        //                order.Street1 + "<br />" +
        //                order.Street2 + "<br />" +
        //                order.CityName + ", " + order.StateOrProvince + " " + order.PostalCode + "<br />" +
        //                order.CountryName + "<br />" +
        //                order.Phone + "</strong></p></td>" +
        //                "    <td align='center' valign='top' width='34%'><p class='titulos'>" + view.Name + "</p>" +
        //                "    <p>" + view.Name + " | eBay</p>" +
        //                "    <p><strong>THANK YOU FOR YOUR ORDER</strong></p>" + GetRepeatedInfo(order.PrintTime) + "</td>" +
        //                "    <td align='right' valign='top' width='33%'><p>" + order.CreatedTime.ToShortDateString() + "</p>" +
        //                "    <p><h2>" + order.SalesRecordNumber + "</h2></p>" +
        //                "    <p><font face='Free 3 of 9 Extended' size='+2'>*" + order.SalesRecordNumber + "*</font></p>" +
        //                "    <p><h2>" + GetShippingInfo(order) + "</h2></p></td>" +
        //                "  </tr>");

        //        stream.Write("</table></center>");

        //        stream.Write("<table width='800' border='1' cellspacing='0' cellpadding='2'><tr><td width='7%'><div align='center'><b>Qty</b></font></div></td><td width='50%'><div align='center'><font><b>Product</b></font></div></td><td width='15%'><div align='center'><font><b>Condition</b></font></div></td> <td width='15%'><div align='center'><font><b>Price</b></font></div></td><td width='10%'><div align='center'><font><b>Total</b></font></div></td></tr>");

        //        foreach (EbayOrderItem orderItem in order.OrderItems)
        //        {
        //            decimal totalPrice = orderItem.QuantityPurchased * orderItem.TransactionPrice;
        //            String listingFormat = orderItem.ListingItem.Listing.Format.Equals("Chinese") ? "[AUCTION] " : "";

        //            string location = orderItem.ListingItem.Item != null ? orderItem.ListingItem.Item.BinLocation : "";
        //            string conditionDisplayName = orderItem.ListingItem.Item != null ? orderItem.ListingItem.Item.GetConditionCode() : "";
        //            string conditionDescription = orderItem.ListingItem.Item != null ? orderItem.ListingItem.Item.ExtendedDescription : "";

        //            stream.Write("  <tr>" +
        //                        "    <td><div align='center'>" + orderItem.QuantityPurchased + "</div></td>" +
        //                        "    <td><div align='left'><b>" + listingFormat + orderItem.ListingItem.Listing.Title + "</b><br />" +
        //                        "        <strong>eBay Item: " + orderItem.Code + " | SKU: " + orderItem.ListingItem.Sku + " | " + location + "</strong></div></td>" +
        //                        "    <td align='center'><b>" + conditionDisplayName + ": " + conditionDescription + "</b></td>" +
        //                        "    <td><div align='right'>" + orderItem.TransactionPrice.ToString("C") + "</div></td>" +
        //                        "    <td><div align='right'>" + totalPrice.ToString("C") + "</div></td>" +
        //                        "  </tr>");
        //        }

        //        stream.Write("<tr><td><div align='center'></div></td><td><div align='left'></div></td><td><div align='left'></div></td>" +
        //                "<td><div align='right'>Shipping:</div></td><td><div align='right'>" +
        //                order.ShippingServiceCost.ToString("C") +
        //                "</div></td></tr>");

        //        if (order.AdjustmentAmount != 0)
        //        {
        //            stream.Write("<tr><td><div align='center'></div></td><td><div align='left'></div></td>" +
        //                        "<td><div align='right'>Discounts:</div></td><td><div align='right'>$" +
        //                        order.AdjustmentAmount + "</div></td></tr>");
        //        }

        //        stream.Write("<tr><td><div align='center'></div></td><td><div align='left'></div></td><td><div align='left'></div></td>" +
        //                    "<td><div align='right'>Order total:</div></td><td><div align='right'>" +
        //                    order.Total.ToString("C") +
        //                    "</div></td></tr></table>");

        //        stream.Write("<table width='100%' align='center' cellpadding='10' cellspacing='0' border='0'>" +
        //                    "<tr><td align='left' valign='top' width='50%'><p><strong>Exchanges:</strong> " +
        //                    "Looking to exchange for a different size? Contact us through eBay messages to  " +
        //                    "see if we can find the right pair for you.</p><p><strong>Returns:</strong> Our return " +
        //                    "policies are stated in our profile page at <strong>eBay.com</strong>. Please contact us through eBay messages <b>first</b> for a Return Authorization number. " +
        //                    "We accept returns within <strong>30 days</strong> after receiving the product. " +
        //                    "The items <strong>must not be worn</strong> and should be returned in their <b>original box</b> and  packaging. " +
        //                    "<u>Buyer is responsible for returning shipping costs</u>.</p>" +
        //                    "<p align='center'><b>PLEASE CONTACT OUR CUSTOMER SERVICE</b></p>" +
        //                    "<p align='center'><b>DEPARTMENT FOR RETURN INSTRUCTIONS</b></p>" +
        //                    "We guarantee a reply to your return request within 24hrs. If you do not receive anything, we recommend checking your spam/junk mail." +
        //                    "</td><td align='left' valign='top' width='50%'>" +
        //                    "<p>To better serve our customers in the future, please write down the reason for returning the item:</p>" +
        //                    "<p>&nbsp;</p><p>&nbsp;</p><p>&nbsp;</p><p>&nbsp;</p><p>&nbsp;</p><p><strong>Once we receive the shoes we will refund your order as stated in the eBay page of the product. " +
        //                    "Inspection and processing usually take </strong><strong>3-5 business days</strong><strong>.</strong><br /><strong>" +
        //                    "We don't refund original  shipping charges.</strong></p></td></tr></table><p class='fineprint'>" +
        //                    "*You MUST INCLUDE THIS PAGE in your return; otherwise we will have trouble  processing your order. " +
        //                    "Contact us through eBay messages for any questions.</p><p align='center'><strong>VISIT OUR WEBSITE</strong></p>");

        //        stream.Write("</td></tr></table>");

        //        if (index % 2 == 0)
        //        {
        //            stream.Write("<p style='page-break-before: always;'>&nbsp;</p>");
        //        }
        //        else
        //        {
        //            stream.Write("<hr style='margin:2px;padding:0px;' />");
        //        }

        //        stream.Flush();
        //    }

        //    var printOrders = orders.Where(p => p.PrintTime.HasValue == false);

        //    string POBoxPattern = @"(?i)\b(?:p(?:ost)?\.?\s*[o0](?:ffice)?\.?\s*b(?:[o0]x)?|b[o0]x)";

        //    var poBoxOrders = printOrders.Where(p => Regex.IsMatch(p.Street1 ?? "", POBoxPattern) || Regex.IsMatch(p.Street2 ?? "", POBoxPattern));

        //    stream.Write("<h1>eBay " + view.Name + " orders summary " + DateTime.Now.ToShortDateString() + "</h1><hr>" +
        //                "<p>&nbsp;<b>TOTAL ORDERS       :" + printOrders.Count() + "</b></p>" +
        //                "<p>&nbsp;<b>TOTAL NEXT DAY     :" + printOrders.Where(p => p.ShippingService.Contains("Overnight") || p.ShippingService.Contains("NextDay")).Count() + "</b></p>" +
        //                "<p>&nbsp;<b>TOTAL PO BOX       :" + poBoxOrders.Count() + "</b></p>" +
        //                "<p>&nbsp;<b>TOTAL INTERNATIONAL:" + printOrders.Where(p => !p.CountryName.Contains("United States")).Count() + "</b></p>" +
        //                "<p>&nbsp;<b>TOTAL ITEMS        :" + printOrders.Sum(p => p.OrderItems.Sum(s => s.QuantityPurchased)) + "</b></p>" +
        //                "<p>&nbsp;<b>TOTAL SHIPPING     :" + printOrders.Sum(p => p.ShippingServiceCost).ToString("C") + "</b></p>" +
        //                "<p>&nbsp;<b>GRAND TOTAL        :" + printOrders.Sum(p => p.Total).ToString("C") + "</b></p>" +
        //                "<p>&nbsp;</p><p>&nbsp;</p><p>DETAILS</p><ul>");


        //    var brands = printOrders.SelectMany(p => p.OrderItems).GroupBy(p => p.ListingItem.Item.SubDescription1).OrderBy(p => p.Key);

        //    foreach (var brand in brands)
        //    {
        //        stream.Write("<li>" + brand.Key + " (" + brand.Sum(s => s.QuantityPurchased) + ") " + string.Format("{0:C}", brand.Sum(s => s.TransactionPrice)) + "</li>");
        //    };

        //    stream.Write("</ul></div></body></html>");

        //    stream.Flush();

        //    stream.Close();
        //}

    }

    public class PendingOrder
    {
        public string MarketplaceName { get; set; }

        public string MarketplaceCode { get; set; }

        public string BuyerName { get; set; }

        public string OrderID { get; set; }

        public List<PendingOrderItem> Items { get; set; }

        public DateTime? PrintTime { get; set; }

        public string AddressLine1 { get; set; }

        public string AddressLine2 { get; set; }

        public string AddressLine3 { get; set; }

        public string City { get; set; }

        public string StateOrRegion { get; set; }

        public string PostalCode { get; set; }

        public string Country { get; set; }

        public string Phone { get; set; }

        public DateTime PurchaseDate { get; set; }

        public string ShippingService { get; set; }

        public decimal ShippingPrice { get; set; }

        public decimal AdjustmentAmount { get; set; }

        public decimal Total { get; set; }
    }

    public class PendingOrderItem
    {
        public string Sku {get; set;}

        public string BinLocation { get; set; }

        public decimal Price { get; set; }

        public decimal Discount { get; set; }

        public int Qty {get; set;}

        public int OnHand { get; set; }

        public string Title { get; set; }
    }
}
