﻿using Cukcuk.Core.DTOs;
using Cukcuk.Core.Entities;
using Cukcuk.Core.Helper;
using Cukcuk.Core.Interfaces.IRepositories;
using Cukcuk.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Http;

namespace Cukcuk.Core.Services
{
    public class CustomerService(ICustomerRepository customerRepository, ICustomerGroupRepository customerGroupRepository,
        IImportRepository importRepository, Cache cacheService) : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository = customerRepository;
        private readonly ICustomerGroupRepository _customerGroupRepository = customerGroupRepository;
        private readonly IImportRepository _importRepository = importRepository;
        private readonly Cache _cacheService = cacheService;
        public async Task AddDataImport(Guid cacheId)
        {
            var customers = _cacheService.GetFromCache<CustomerImportResponse>(cacheId);
            foreach (var customer in customers)
            {
                customer.CustomerId = Guid.NewGuid();
                await _customerRepository.Create(customer);
            }
        }

        public async Task Create(Customer customer)
        {
            customer.CustomerId = Guid.NewGuid();
            await _customerRepository.Create(customer);
        }

        public byte[] CreateExcelFile(List<Customer> customers)
        {
            var customerExports = new List<CustomerExport>();
            var index = 1;
            foreach (var customer in customers)
            {
                customerExports.Add(new CustomerExport
                {
                    STT = index++,
                    CustomerCode = customer.CustomerCode,
                    Fullname = customer.Fullname,
                    DateOfBirth = customer.DateOfBirth,
                    Gender = customer.GenderName,
                    Address = customer.Address,
                    MobileNumber = customer.MobileNumber,
                    Email = customer.Email,
                    Amount = customer.Amount,
                    CustomerGroup = customer.GroupName,
                });
            }

            var file = ExcelHelper.CreateFile(customerExports);
            return file;
        }

        public byte[] CreateResultFile(Guid? validCacheId, Guid InvalidCacheId)
        {
            var customers = new List<CustomerImportResponse>();
            if (validCacheId != null) customers.AddRange(_cacheService.GetFromCache<CustomerImportResponse>(validCacheId));
            customers.AddRange(_cacheService.GetFromCache<CustomerImportResponse>(InvalidCacheId));

            var customerReport = new List<CustomerImportReport>();
            var index = 1;

            foreach (var customer in customers)
            {
                customerReport.Add(new CustomerImportReport
                {
                    STT = index++,
                    CustomerCode = customer.CustomerCode,
                    Fullname = customer.Fullname,
                    DateOfBirth = customer.DateOfBirth,
                    Gender = customer.GenderName,
                    Address = customer.Address,
                    MobileNumber = customer.MobileNumber,
                    Email = customer.Email,
                    Amount = customer.Amount,
                    CustomerGroup = customer.GroupName,
                    Status = customer.Status,
                    Errors = customer.Errors,
                });
            }

            var file = ExcelHelper.CreateFile(customerReport);
            return file;
        }

        public Task DeleteById(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<object> FilterCustomer(int pageSize, int pageNumber, string? keyword)
        {
            var (customers, totalRecords) = await _customerRepository.FilterCustomers(pageSize, pageNumber, keyword);
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var response = new
            {
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                PageSize = pageSize,
                CurrentPage = pageNumber,
                Data = customers
            };

            return response;
        }

        public Task<IEnumerable<Customer?>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<Customer?> GetById(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetNewCustomerCode()
        {
            throw new NotImplementedException();
        }

        public async Task<object> ImportExcelFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No files were provided.");

            var imports = await _importRepository.GetByTable("Customer");

            var customers = await ExcelHelper.ReadFile<CustomerImportResponse>(file, imports);

            var groups = await _customerGroupRepository.FindAll();

            var listCustomerCode = new List<string>();
            var listMobileNumber = new List<string>();
            var customerValid = new List<CustomerImportResponse>();
            var customerInvalid = new List<CustomerImportResponse>();

            foreach (var customer in customers)
            {
                await ExecuteCustomerData(customer, groups, listCustomerCode, listMobileNumber, _customerRepository);

                if (!customer.Status)
                {
                    customerInvalid.Add(customer);
                }
                else
                {
                    customerValid.Add(customer);
                }
            }

            var cacheId = Guid.NewGuid();
            var invalidCacheId = Guid.NewGuid();

            _cacheService.SaveDataToCache<CustomerImportResponse>(cacheId, customerValid);
            _cacheService.SaveDataToCache<CustomerImportResponse>(invalidCacheId, customerInvalid);

            return new
            {
                TotalValid = customerValid.Count,
                TotalInvalid = customerInvalid.Count,
                Datas = customers,
                ValidDataCacheId = cacheId,
                InvalidDataCacheId = invalidCacheId
            };
        }

        private static async Task ExecuteCustomerData(CustomerImportResponse customer,  IEnumerable<CustomerGroup> groups,
                               List<string> listCustomerCode, List<string> listMobileNumber, ICustomerRepository _customerRepository)
        {
            customer.Status = true;
            if (customer.DateOfBirth == null)
            {
                customer.Errors.Add("Ngày sinh không hợp lệ");
                customer.Status = false;
            }

            var checkCodeSystem = await _customerRepository.CheckCustomerCode(customer.CustomerCode);
            if (checkCodeSystem)
            {
                customer.Errors.Add("Mã khách hàng đã tồn tại trong hệ thống");
                customer.Status = false;
            }

            var checkCodeTable = listCustomerCode.Any(code => code == customer.CustomerCode);
            if (checkCodeTable)
            {
                customer.Errors.Add("Mã khách hàng trùng với khách hàng khác trong tệp nhập khẩu");
                customer.Status = false;
            }

            var checkMobileSys = await _customerRepository.CheckMobileNumber(customer.MobileNumber);
            if (checkMobileSys)
            {
                customer.Errors.Add("Số điện thoại đã tồn tại trong hệ thống");
                customer.Status = false;
            }

            var checkMobileTable = listMobileNumber.Any(mobile => mobile == customer.MobileNumber);
            if (checkMobileTable)
            {
                customer.Errors.Add("Số điện thoại trùng với số điện thoại của khách hàng khác trong tệp nhập khẩu");
                customer.Status = false;
            }

            var group = groups.Where(d => d.GroupName == customer.GroupName).FirstOrDefault();
            if (group == null)
            {
                customer.Errors.Add("Nhóm khách hàng không có trong hệ thống");
                customer.Status = false;
            }
            else
            {
                customer.GroupId = group.GroupId;
            }

            listCustomerCode.Add(customer.CustomerCode);
            listMobileNumber.Add(customer.MobileNumber);
        }

        public Task Update(Guid id, Customer entity)
        {
            throw new NotImplementedException();
        }
    }
}
