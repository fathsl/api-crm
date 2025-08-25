using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using crmApi.Models;

namespace crmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportingController : ControllerBase
    {
        private readonly string _connectionString;

        public ReportingController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet("product-categories")]
        public async Task<ActionResult<List<ProductCategoryDto>>> GetProductCategories()
        {
            var categories = new List<ProductCategoryDto>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                string query = "SELECT KategoriAdi, Stok FROM UrunKategorileri";

                using var command = new MySqlCommand(query, connection);
                using var adapter = new MySqlDataAdapter(command);
                var dataTable = new DataTable();

                adapter.Fill(dataTable);

                foreach (DataRow row in dataTable.Rows)
                {
                    categories.Add(new ProductCategoryDto
                    {
                        CategoryName = row["KategoriAdi"].ToString(),
                        Stock = Convert.ToInt32(row["Stok"])
                    });
                }

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error loading data: {ex.Message}");
            }
        }

        [HttpGet("components")]
        public async Task<ActionResult<List<ComponentDto>>> GetComponents()
        {
            var components = new List<ComponentDto>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                string query = "SELECT BilesenID, BilesenAdi, Stok FROM BilesenBilgileri";

                using var command = new MySqlCommand(query, connection);
                using var adapter = new MySqlDataAdapter(command);
                var dataTable = new DataTable();

                adapter.Fill(dataTable);

                foreach (DataRow row in dataTable.Rows)
                {
                    components.Add(new ComponentDto
                    {
                        ComponentId = Convert.ToInt32(row["BilesenID"]),
                        ComponentName = row["BilesenAdi"].ToString(),
                        Stock = Convert.ToInt32(row["Stok"])
                    });
                }

                return Ok(components);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error loading data: {ex.Message}");
            }
        }

        [HttpGet("customer-purchases")]
        public async Task<ActionResult<List<dynamic>>> GetCustomerPurchases()
        {
            var purchases = new List<dynamic>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                string query = @"
                    SELECT 
                        mb.MusteriAd, 
                        SUM(tb.Miktar) AS ToplamMiktar
                    FROM 
                        TeslimatBilgileri tb
                    JOIN 
                        MusteriBilgileri mb ON tb.MusteriID = mb.MusteriID
                    GROUP BY 
                        tb.MusteriID
                    ORDER BY 
                        ToplamMiktar DESC";

                using var command = new MySqlCommand(query, connection);
                using var adapter = new MySqlDataAdapter(command);
                var dataTable = new DataTable();

                adapter.Fill(dataTable);

                foreach (DataRow row in dataTable.Rows)
                {
                    purchases.Add(new
                    {
                        CustomerName = row["MusteriAd"].ToString(),
                        TotalQuantity = Convert.ToInt32(row["ToplamMiktar"])
                    });
                }

                return Ok(purchases);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error loading data: {ex.Message}");
            }
        }

        [HttpGet("country-deliveries")]
        public async Task<ActionResult<List<dynamic>>> GetCountryDeliveries()
        {
            var deliveries = new List<dynamic>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                string query = @"
                    SELECT 
                        ub.UlkeAdi, 
                        SUM(tb.Miktar) AS ToplamMiktar
                    FROM 
                        TeslimatBilgileri tb
                    JOIN 
                        MusteriBilgileri mb ON tb.MusteriID = mb.MusteriID
                    JOIN 
                        UlkelerBilgileri ub ON mb.UlkelerID = ub.UlkelerID
                    GROUP BY 
                        ub.UlkeAdi
                    ORDER BY 
                        ToplamMiktar DESC";

                using var command = new MySqlCommand(query, connection);
                using var adapter = new MySqlDataAdapter(command);
                var dataTable = new DataTable();

                adapter.Fill(dataTable);

                foreach (DataRow row in dataTable.Rows)
                {
                    deliveries.Add(new
                    {
                        CountryName = row["UlkeAdi"].ToString(),
                        TotalQuantity = Convert.ToInt32(row["ToplamMiktar"])
                    });
                }

                return Ok(deliveries);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error loading data: {ex.Message}");
            }
        }

        [HttpGet("region-deliveries")]
        public async Task<ActionResult<List<dynamic>>> GetRegionDeliveries()
        {
            var deliveries = new List<dynamic>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                string query = @"
                    SELECT 
                        ub.Bolge, 
                        SUM(tb.Miktar) AS ToplamMiktar
                    FROM 
                        TeslimatBilgileri tb
                    JOIN 
                        MusteriBilgileri mb ON tb.MusteriID = mb.MusteriID
                    JOIN 
                        UlkelerBilgileri ub ON mb.UlkelerID = ub.UlkelerID
                    GROUP BY 
                        ub.Bolge
                    ORDER BY 
                        ToplamMiktar DESC";

                using var command = new MySqlCommand(query, connection);
                using var adapter = new MySqlDataAdapter(command);
                var dataTable = new DataTable();

                adapter.Fill(dataTable);

                foreach (DataRow row in dataTable.Rows)
                {
                    deliveries.Add(new
                    {
                        Region = row["Bolge"].ToString(),
                        TotalQuantity = Convert.ToInt32(row["ToplamMiktar"])
                    });
                }

                return Ok(deliveries);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error loading data: {ex.Message}");
            }
        }

        [HttpGet("customers")]
        public async Task<ActionResult<List<CustomerDto>>> GetCustomers()
        {
            var customers = new List<CustomerDto>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                string query = @"
                    SELECT 
                        m.MusteriAd, 
                        m.Telefon, 
                        m.E_Mail, 
                        m.VATNumarasi, 
                        m.Adres, 
                        u.UlkeAdi, 
                        u.Bolge
                    FROM 
                        MusteriBilgileri m
                    JOIN 
                        UlkelerBilgileri u ON m.UlkelerID = u.UlkelerID";

                using var command = new MySqlCommand(query, connection);
                using var adapter = new MySqlDataAdapter(command);
                var dataTable = new DataTable();

                adapter.Fill(dataTable);

                foreach (DataRow row in dataTable.Rows)
                {
                    customers.Add(new CustomerDto
                    {
                        CustomerName = row["MusteriAd"].ToString(),
                        Phone = row["Telefon"].ToString(),
                        Email = row["E_Mail"].ToString(),
                        VatNumber = row["VATNumarasi"].ToString(),
                        Address = row["Adres"].ToString(),
                        CountryName = row["UlkeAdi"].ToString(),
                        Region = row["Bolge"].ToString()
                    });
                }

                return Ok(customers);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error loading data: {ex.Message}");
            }
        }



        [HttpGet("orders")]
        public async Task<ActionResult<List<OrderDto>>> GetOrders()
        {
            var orders = new List<OrderDto>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                string query = @"
                    SELECT 
                        SiparisAlID, 
                        SiparisNo, 
                        MusteriAd, 
                        MUlke,
                        ToplamFiyat,
                        GREATEST((ToplamFiyat * (PesinYüzde / 100)) - COALESCE(OdenenMiktar, 0), 0) AS KaparoFiyat, 
                        ToplamFiyat - COALESCE(OdenenMiktar, 0) AS KalanBakiye,  
                        PesinYüzde, 
                        COALESCE(TeslimatÇeşiti, 'Veri Yok') AS TeslimatÇeşiti,
                        ParaTipi,
                        OdemeDurum,
                        SiparisMiTeklifMi,
                        CASE 
                            WHEN Tarih IS NULL THEN 'HATA' 
                            ELSE DATE_FORMAT(Tarih, '%d/%m/%Y') 
                        END AS Tarih
                    FROM 
                        SiparisAlTablo
                    WHERE 
                        Kontrol = 'Onay' AND SiparisMiTeklifMi != 'Teklif'
                    ORDER BY SiparisAlID DESC";

                using var command = new MySqlCommand(query, connection);
                using var adapter = new MySqlDataAdapter(command);
                var dataTable = new DataTable();

                adapter.Fill(dataTable);

                foreach (DataRow row in dataTable.Rows)
                {
                    orders.Add(new OrderDto
                    {
                        OrderId = Convert.ToInt32(row["SiparisAlID"]),
                        OrderNumber = row["SiparisNo"].ToString(),
                        CustomerName = row["MusteriAd"].ToString(),
                        Country = row["MUlke"].ToString(),
                        TotalPrice = Convert.ToDecimal(row["ToplamFiyat"]),
                        DepositPrice = Convert.ToDecimal(row["KaparoFiyat"]),
                        RemainingBalance = Convert.ToDecimal(row["KalanBakiye"]),
                        AdvancePercentage = Convert.ToDecimal(row["PesinYüzde"]),
                        DeliveryType = row["TeslimatÇeşiti"].ToString(),
                        CurrencyType = row["ParaTipi"].ToString(),
                        PaymentStatus = row["OdemeDurum"].ToString(),
                        OrderType = row["SiparisMiTeklifMi"].ToString(),
                        OrderDate = row["Tarih"].ToString()
                    });
                }

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error loading data: {ex.Message}");
            }
        }
        
        [HttpPost("orders/filter")]
        public async Task<ActionResult<List<FilteredOrderDto>>> FilterOrders([FromBody] OrderFilterDto filter)
        {
            var orders = new List<FilteredOrderDto>();
            
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                
                // Handle order number transformation (T -> S)
                string orderNumber = filter.OrderNumber?.Trim();
                if (!string.IsNullOrEmpty(orderNumber) && orderNumber.StartsWith("T"))
                {
                    orderNumber = "S" + orderNumber.Substring(1);
                }

                // Build dynamic query
                string query = @"
                    SELECT 
                        CASE 
                            WHEN SiparisMiTeklifMi = 'Teklif' THEN TeklifNo
                            ELSE SiparisNo  
                        END AS Numara, 
                        MusteriAd,
                        MUlke,
                        kb.KullaniciAdi AS Kullanici,
                        SiparisMiTeklifMi AS 'Aşama',
                        Kontrol,
                        ToplamFiyat,
                        GREATEST((ToplamFiyat * (PesinYüzde / 100)) - COALESCE(OdenenMiktar, 0), 0) AS KaparoFiyat, 
                        ToplamFiyat - COALESCE(OdenenMiktar, 0) AS KalanBakiye,  
                        PesinYüzde, 
                        COALESCE(TeslimatÇeşiti, 'Veri Yok') AS TeslimatÇeşiti,
                        ParaTipi,
                        OdemeDurum,
                        Tarih,
                        CASE 
                            WHEN SiparisMiTeklifMi = 'Uretim' THEN STarih
                            ELSE NULL
                        END AS 'STarih'
                    FROM SiparisAlTablo sat
                    INNER JOIN KullaniciBilgileri kb ON sat.KullaniciID = kb.KullaniciID
                    WHERE 1=1";

                var parameters = new List<MySqlParameter>();

                // Add filters dynamically
                if (!string.IsNullOrEmpty(orderNumber))
                {
                    query += " AND sat.SiparisNo LIKE @OrderNumber";
                    parameters.Add(new MySqlParameter("@OrderNumber", $"%{orderNumber}%"));
                }

                if (!string.IsNullOrEmpty(filter.CustomerName))
                {
                    query += " AND sat.MusteriAd LIKE @CustomerName";
                    parameters.Add(new MySqlParameter("@CustomerName", $"%{filter.CustomerName}%"));
                }

                if (filter.UserId.HasValue && filter.UserId > 0)
                {
                    query += " AND sat.KullaniciID = @UserId";
                    parameters.Add(new MySqlParameter("@UserId", filter.UserId.Value));
                }

                if (!string.IsNullOrEmpty(filter.Country))
                {
                    query += " AND sat.MUlke = @Country";
                    parameters.Add(new MySqlParameter("@Country", filter.Country));
                }

                if (!string.IsNullOrEmpty(filter.PaymentStatus))
                {
                    query += " AND sat.OdemeDurum = @PaymentStatus";
                    parameters.Add(new MySqlParameter("@PaymentStatus", filter.PaymentStatus));
                }

                if (!string.IsNullOrEmpty(filter.ProcessStatus))
                {
                    query += " AND sat.SiparisMiTeklifMi = @ProcessStatus";
                    parameters.Add(new MySqlParameter("@ProcessStatus", filter.ProcessStatus));
                }

                if (filter.DeliveryDateStart.HasValue && filter.DeliveryDateEnd.HasValue)
                {
                    query += " AND sat.Tarih BETWEEN @DateStart AND @DateEnd";
                    parameters.Add(new MySqlParameter("@DateStart", filter.DeliveryDateStart.Value.Date));
                    parameters.Add(new MySqlParameter("@DateEnd", filter.DeliveryDateEnd.Value.Date));
                }

                query += " ORDER BY sat.SiparisAlID DESC";

                await connection.OpenAsync();
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddRange(parameters.ToArray());

                using var adapter = new MySqlDataAdapter(command);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                foreach (DataRow row in dataTable.Rows)
                {
                    orders.Add(new FilteredOrderDto
                    {
                        Number = row["Numara"].ToString(),
                        CustomerName = row["MusteriAd"].ToString(),
                        Country = row["MUlke"].ToString(),
                        User = row["Kullanici"].ToString(),
                        Stage = row["Aşama"].ToString(),
                        Control = row["Kontrol"].ToString(),
                        TotalPrice = Convert.ToDecimal(row["ToplamFiyat"]),
                        DepositPrice = Convert.ToDecimal(row["KaparoFiyat"]),
                        RemainingBalance = Convert.ToDecimal(row["KalanBakiye"]),
                        AdvancePercentage = Convert.ToDecimal(row["PesinYüzde"]),
                        DeliveryType = row["TeslimatÇeşiti"].ToString(),
                        CurrencyType = row["ParaTipi"].ToString(),
                        PaymentStatus = row["OdemeDurum"].ToString(),
                        OrderDate = row["Tarih"] != DBNull.Value ? Convert.ToDateTime(row["Tarih"]) : null,
                        ProductionDate = row["STarih"] != DBNull.Value ? Convert.ToDateTime(row["STarih"]) : null
                    });
                }

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error filtering orders: {ex.Message}");
            }
        }

        [HttpGet("filter-options")]
        public async Task<ActionResult<object>> GetFilterOptions()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var customers = new List<object>();
                string customerQuery = "SELECT MusteriID, MusteriAd FROM MusteriBilgileri ORDER BY MusteriAd ASC";
                using var customerCmd = new MySqlCommand(customerQuery, connection);
                using var customerAdapter = new MySqlDataAdapter(customerCmd);
                var customerTable = new DataTable();
                customerAdapter.Fill(customerTable);

                foreach (DataRow row in customerTable.Rows)
                {
                    customers.Add(new { Id = row["MusteriID"], Name = row["MusteriAd"].ToString() });
                }

                // Get countries
                var countries = new List<string>();
                string countryQuery = "SELECT DISTINCT MUlke FROM SiparisAlTablo WHERE MUlke IS NOT NULL ORDER BY MUlke";
                using var countryCmd = new MySqlCommand(countryQuery, connection);
                using var countryReader = await countryCmd.ExecuteReaderAsync();
                while (await countryReader.ReadAsync())
                {
                    countries.Add(countryReader["MUlke"].ToString());
                }
                countryReader.Close();

                var users = new List<object>();
                string userQuery = "SELECT KullaniciID, KullaniciAdi FROM KullaniciBilgileri ORDER BY KullaniciAdi";
                using var userCmd = new MySqlCommand(userQuery, connection);
                using var userAdapter = new MySqlDataAdapter(userCmd);
                var userTable = new DataTable();
                userAdapter.Fill(userTable);

                foreach (DataRow row in userTable.Rows)
                {
                    users.Add(new { Id = row["KullaniciID"], Name = row["KullaniciAdi"].ToString() });
                }

                return Ok(new
                {
                    Customers = customers,
                    Countries = countries,
                    Users = users,
                    PaymentStatuses = new[] { "Ödendi", "Ödenmedi", "Kısmi Ödendi" },
                    ProcessStatuses = new[] { "Sipariş", "Teklif", "Üretim", "Teslim Edildi" }
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error getting filter options: {ex.Message}");
            }
        }

    }
}