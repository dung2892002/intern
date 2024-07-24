﻿using BE__Back_End_.Models;
using BE__Back_End_.Payloads.DTOs;
using BE__Back_End_.Payloads.Request;
using BE__Back_End_.Services;
using BE__Back_End_.Services.IService;
using Dapper;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace BE__Back_End_.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [EnableCors("AllowAll")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly IPositionService _positionService;
        private readonly IDepartmentService _departmentService;

        public EmployeesController(IEmployeeService employeeService, IDepartmentService departmentService, IPositionService positionService)
        {
            _employeeService = employeeService;
            _departmentService = departmentService;
            _positionService = positionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            try
            {
                var employees = await _employeeService.GetEmployees();
                return StatusCode(200, employees);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployee(Guid id)
        {
            try
            {
                
                var employee = await _employeeService.GetEmployeeById(id);
                if (employee == null)
                {
                    return StatusCode(404, "Employee not exist");
                }

                return StatusCode(200, employee);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] EmployeeRequest employeeRequest)
        {
            try
            {
                if (employeeRequest.PositionId == Guid.Empty || employeeRequest.DepartmentId == Guid.Empty)
                {
                    return StatusCode(400, "PositionId and DepartmentId are required");
                }

                var existingDepartment = await _departmentService.GetDepartmentById(employeeRequest.DepartmentId);
                if (existingDepartment == null)
                {
                    return StatusCode(404, "Department not exists");
                }

                var existingPosition = await _positionService.GetPositionById(employeeRequest.PositionId);
                if (existingPosition == null)
                {
                    return StatusCode(404, "Position not exists");
                }

                await _employeeService.CreateEmployee(employeeRequest);

                return StatusCode(201, "Created employee succesfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] EmployeeRequest employeeRequest)
        {
            try
            {
                if (employeeRequest.PositionId == Guid.Empty || employeeRequest.DepartmentId == Guid.Empty)
                {
                    return StatusCode(400, "PositionId and DepartmentId are required");
                }

                var existingDepartment = await _departmentService.GetDepartmentById(employeeRequest.DepartmentId);
                if (existingDepartment == null)
                {
                    return StatusCode(404, "Department not exists");
                }

                var existingPosition = await _positionService.GetPositionById(employeeRequest.PositionId);
                if (existingPosition == null)
                {
                    return StatusCode(404, "Position not exists");
                }

                var existingEmployee = await _employeeService.GetEmployeeById(id);
                if (existingEmployee == null)
                {
                    return StatusCode(404, "Employee not exists");
                }

                await _employeeService.UpdateEmployee(id, employeeRequest);

                return StatusCode(200, "Update employee successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(Guid id)
        {
            try
            {
                var existingEmployee = await _employeeService.GetEmployeeById(id);
                if (existingEmployee == null)
                {
                    return StatusCode(404, "Employee not exists");
                }

                await _employeeService.DeleteEmployee(id);

                return StatusCode(200, "Delete employee successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("NewEmployeeCode")]
        public async Task<IActionResult> GetNewEmployeeCode()
        {
            try
            {
                var newEmployeeCode = await _employeeService.GetNewEmployeeCode();
                return StatusCode(200, newEmployeeCode);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("filter")]
        public async Task<IActionResult> FilterEmployee(int pageSize, int pageNumber, string? employeeFilter, Guid? departmentId, Guid? positionId)
        {
            try
            {
                var response = await _employeeService.FilterEmployees(pageSize, pageNumber, employeeFilter, departmentId, positionId);

                return StatusCode(200, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
