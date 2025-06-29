﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObjects;
using Services;
using System.Windows;
using HoangLeThanhDucWPF.Commands;

namespace HoangLeThanhDucWPF.ViewModels
{
    public class AdminViewModel : ViewModelBase
    {
        private readonly ICustomerService icustomerService;
        private readonly IProductService iproductService;
        private readonly IOrderService iorderService;
        private readonly IOrderDetailService iorderDetailService;

        public event Action RequestClose;

        // --- Properties for Data Binding ---
        private ObservableCollection<Customer> _customers;

        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set { _customers = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Product> _products;
        public ObservableCollection<Product> Products
        {
            get => _products;
            set { _products = value; OnPropertyChanged(); }
        }

        // Orders collection for Order Management tab - always shows all orders
        private ObservableCollection<Order> _orders;
        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set { _orders = value; OnPropertyChanged(); }
        }

        // Separate collection for Reports tab - shows filtered orders
        private ObservableCollection<Order> _reportOrders;
        public ObservableCollection<Order> ReportOrders
        {
            get => _reportOrders;
            set { _reportOrders = value; OnPropertyChanged(); }
        }

        private ObservableCollection<OrderDetail> _selectedOrderDetails;
        public ObservableCollection<OrderDetail> SelectedOrderDetails
        {
            get => _selectedOrderDetails;
            set { _selectedOrderDetails = value; OnPropertyChanged(); CalculateOrderTotal(); }
        }

        private double _orderTotal;
        public double OrderTotal
        {
            get => _orderTotal;
            set { _orderTotal = value; OnPropertyChanged(); }
        }

        // --- Search Properties ---
        #region Search Properties
        // Thuộc tính để binding với ô TextBox tìm kiếm Customer
        private string _searchCustomerText = "";
        public string SearchCustomerText
        {
            get => _searchCustomerText;
            set { _searchCustomerText = value; OnPropertyChanged(); }
        }

        // Thuộc tính để binding với ô TextBox tìm kiếm Product
        private string _searchProductText = "";
        public string SearchProductText
        {
            get => _searchProductText;
            set { _searchProductText = value; OnPropertyChanged(); }
        }
        #endregion


        #region Report Properties
        // Gán giá trị mặc định cho ngày bắt đầu và kết thúc
        public DateTime ReportStartDate { get; set; } = DateTime.Now.AddMonths(-1); // Mặc định là 1 tháng trước
        public DateTime ReportEndDate { get; set; } = DateTime.Now; // Mặc định là hôm nay
        #endregion

        

        // Selected Item Properties
        #region Selected Item Properties
        // Thuộc tính để binding với item được chọn trong bảng Customer
        private Customer? _selectedCustomer;
        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set { _selectedCustomer = value; OnPropertyChanged(); }
        }

        // Thuộc tính để binding với item được chọn trong bảng Product
        private Product? _selectedProduct;
        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; OnPropertyChanged(); }
        }

        private Order? _selectedOrder;
        public Order? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                _selectedOrder = value;
                // Khi một Order được chọn, tải các OrderDetail tương ứng
                LoadSelectedOrderDetails();
                OnPropertyChanged();
            }
        }
        #endregion


        // --- Commands ---
        #region Commands 
        public ICommand DeleteCustomerCommand { get; private set; }
        public ICommand DeleteProductCommand { get; private set; }

        public ICommand AddCustomerCommand { get; private set; }
        public ICommand UpdateCustomerCommand { get; private set; }
      
        public ICommand AddProductCommand { get; private set; }
        public ICommand UpdateProductCommand { get; private set; }

        public ICommand CreateOrderCommand { get; private set; }

        public ICommand SearchCustomerCommand { get; private set; }
        public ICommand SearchProductCommand { get; private set; }

        public ICommand LogoutCommand { get; private set; }

        public ICommand GenerateReportCommand { get; private set; }

        #endregion




        public AdminViewModel()
        {
            icustomerService = new CustomerService();
            iproductService = new ProductService();
            iorderService = new OrderService();
            iorderDetailService = new OrderDetailService();
            LoadData();

            // Khởi tạo các Command và trỏ chúng tới các phương thức xử lý
            SearchCustomerCommand = new RelayCommand(ExecuteSearchCustomer);
            SearchProductCommand = new RelayCommand(ExecuteSearchProduct);


            // Khởi tạo Delete Commands
            DeleteCustomerCommand = new RelayCommand(ExecuteDeleteCustomer, CanExecuteUpdateOrDelete);
            DeleteProductCommand = new RelayCommand(ExecuteDeleteProduct, CanExecuteUpdateOrDeleteProduct);


            // Khởi tạo Add và Update Commands
            AddCustomerCommand = new RelayCommand(ExecuteAddCustomer);
            UpdateCustomerCommand = new RelayCommand(ExecuteUpdateCustomer, CanExecuteUpdateOrDelete);

            // Khởi tạo các Command cho Product
            AddProductCommand = new RelayCommand(ExecuteAddProduct);
            UpdateProductCommand = new RelayCommand(ExecuteUpdateProduct, CanExecuteUpdateOrDeleteProduct);

            // Khởi tạo Order Commands
            CreateOrderCommand = new RelayCommand(ExecuteCreateOrder);

            GenerateReportCommand = new RelayCommand(ExecuteGenerateReport);


            // Khởi tạo LogoutCommand
            LogoutCommand = new RelayCommand(ExecuteLogout);
        }

        private void LoadData()
        {
            Customers = new ObservableCollection<Customer>(icustomerService.GetCustomers());
            Products = new ObservableCollection<Product>(iproductService.GetProducts());
            Orders = new ObservableCollection<Order>(iorderService.GetOrders());
            
            // Initialize ReportOrders with all orders initially
            ReportOrders = new ObservableCollection<Order>(iorderService.GetOrders());
        }

        private void LoadSelectedOrderDetails()
        {
            if (SelectedOrder != null)
            {
                // Lấy tất cả OrderDetail và lọc ra những cái có OrderId trùng khớp
                var allDetails = iorderDetailService.GetOrderDetails();
                var filteredDetails = allDetails.Where(od => od.OrderId == SelectedOrder.OrderId).ToList();
                
                SelectedOrderDetails = new ObservableCollection<OrderDetail>(filteredDetails);
            }
            else
            {
                // Nếu không có order nào được chọn, làm trống danh sách details
                SelectedOrderDetails = new ObservableCollection<OrderDetail>();
            }
        }

        private void CalculateOrderTotal()
        {
            if (SelectedOrderDetails != null && SelectedOrderDetails.Any())
            {
                OrderTotal = SelectedOrderDetails.Sum(od => od.UnitPrice * od.Quantity * (1 - od.Discount));
            }
            else
            {
                OrderTotal = 0;
            }
        }

        private void ExecuteLogout(object? obj)
        {
            // Gửi yêu cầu đóng cửa sổ
            RequestClose?.Invoke();
        }

        // --- Các phương thức thực thi Command ---
        private void ExecuteSearchCustomer(object? obj)
        {
            // Cập nhật lại danh sách Customers dựa trên từ khóa tìm kiếm
            Customers = new ObservableCollection<Customer>(icustomerService.SearchCustomers(SearchCustomerText));
        }

        private void ExecuteSearchProduct(object? obj)
        {
            // Cập nhật lại danh sách Products dựa trên từ khóa tìm kiếm
            Products = new ObservableCollection<Product>(iproductService.SearchProducts(SearchProductText));
        }



        #region Delete Command Methods

        // Phương thức kiểm tra điều kiện để thực thi lệnh Delete or Update
        private bool CanExecuteUpdateOrDelete(object? obj)
        {
            return SelectedCustomer != null;
        }


        // Hành động khi nhấn nút Delete Customer
        private void ExecuteDeleteCustomer(object? obj)
        {
            if (SelectedCustomer == null) return;

            // Hiển thị hộp thoại xác nhận
            if (MessageBox.Show($"Are you sure you want to delete '{SelectedCustomer.ContactName}'?",
                                "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                icustomerService.DeleteCustomer(SelectedCustomer.CustomerId);
                LoadData(); // Tải lại dữ liệu
                MessageBox.Show("Customer deleted successfully.", "Success");
            }
        }

        // Hành động khi nhấn nút Delete Product
        private void ExecuteDeleteProduct(object? obj)
        {
            if (SelectedProduct == null) return;

            if (MessageBox.Show($"Are you sure you want to delete '{SelectedProduct.ProductName}'?",
                                "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                iproductService.DeleteProduct(SelectedProduct.ProductId);
                LoadData(); // Tải lại dữ liệu
                MessageBox.Show("Product deleted successfully.", "Success");
            }
        }

        #endregion


        #region Add/Update Command Methods

        private void ExecuteAddCustomer(object? obj)
        {
            var newCustomer = new Customer(0, "", "", "", "", ""); // Tạo customer rỗng
            var vm = new CustomerDetailViewModel(newCustomer);
            var detailWindow = new CustomerDetailWindow(vm);

            if (detailWindow.ShowDialog() == true)
            {
                icustomerService.AddCustomer(vm.Customer);
                LoadData(); // Tải lại danh sách
                MessageBox.Show("Customer added successfully.", "Success");
            }
        }

        private void ExecuteUpdateCustomer(object? obj)
        {
            if (SelectedCustomer == null) return;

            // Tạo một bản sao của customer để chỉnh sửa, tránh ảnh hưởng trực tiếp đến list
            var customerToUpdate = (Customer)SelectedCustomer.Clone();
            var vm = new CustomerDetailViewModel(customerToUpdate);
            var detailWindow = new CustomerDetailWindow(vm);

            if (detailWindow.ShowDialog() == true)
            {
                icustomerService.UpdateCustomer(vm.Customer);
                LoadData(); // Tải lại danh sách
                MessageBox.Show("Customer updated successfully.", "Success");
            }
        }

        #endregion


        #region Product Command Methods

        private bool CanExecuteUpdateOrDeleteProduct(object? obj)
        {
            return SelectedProduct != null;
        }

        private void ExecuteAddProduct(object? obj)
        {
            var newProduct = new Product { ProductId = 0, ProductName = "", CategoryId = 0, UnitPrice = 0, UnitsInStock = 0 };
            var vm = new ProductDetailViewModel(newProduct);
            var detailWindow = new ProductDetailWindow(vm);

            if (detailWindow.ShowDialog() == true)
            {
                iproductService.AddProduct(vm.Product);
                LoadData();
                MessageBox.Show("Product added successfully.", "Success");
            }
        }

        private void ExecuteUpdateProduct(object? obj)
        {
            if (SelectedProduct == null) return;

            var productToUpdate = (Product)SelectedProduct.Clone();
            var vm = new ProductDetailViewModel(productToUpdate);
            var detailWindow = new ProductDetailWindow(vm);

            if (detailWindow.ShowDialog() == true)
            {
                iproductService.UpdateProduct(vm.Product);
                LoadData();
                MessageBox.Show("Product updated successfully.", "Success");
            }
        }

        #endregion

        #region Order Command Methods

        private void ExecuteCreateOrder(object? obj)
        {
            var newOrder = new Order 
            { 
                OrderId = 0, 
                CustomerId = 0, 
                EmployeeId = 1, // Default employee ID
                OrderDate = DateTime.Now 
            };
            
            var vm = new OrderDetailViewModel(newOrder);
            var detailWindow = new OrderDetailWindow(vm);

            if (detailWindow.ShowDialog() == true)
            {
                // Save the order first - the DAO will auto-assign the ID
                iorderService.AddOrder(vm.Order);
                
                // Get the updated order with the new ID
                var savedOrderId = vm.Order.OrderId;
                
                // Save all order details
                foreach (var detail in vm.OrderDetails)
                {
                    detail.OrderId = savedOrderId;
                    iorderDetailService.AddOrderDetail(detail);
                }

                LoadData(); // Reload data
                MessageBox.Show($"Order created successfully with ID: {savedOrderId}", "Success");
            }
        }

        #endregion

        #region Report Command Methods
        private void ExecuteGenerateReport(object? obj)
        {
            // Kiểm tra nếu ngày kết thúc nhỏ hơn ngày bắt đầu
            if (ReportEndDate < ReportStartDate)
            {
                MessageBox.Show("End Date cannot be earlier than Start Date.", "Invalid Date Range", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Gọi service để lấy dữ liệu đã lọc và sắp xếp
            var reportData = iorderService.GetOrdersByPeriod(ReportStartDate, ReportEndDate);

            // Update ONLY ReportOrders collection, NOT Orders collection
            ReportOrders = new ObservableCollection<Order>(reportData);

            MessageBox.Show($"Report generated successfully. Found {reportData.Count} orders.", "Success");
        }
        #endregion
    }
}
