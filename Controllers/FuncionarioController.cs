using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using TrilhaNetAzureDesafio.Context;
using TrilhaNetAzureDesafio.Models;

namespace TrilhaNetAzureDesafio.Controllers;

[ApiController]
[Route("[controller]")]
public class FuncionarioController : ControllerBase
{
    private readonly RHContext _context;
    private readonly string _connectionString;
    private readonly string _tableName;

    public FuncionarioController(RHContext context, IConfiguration configuration)
    {
        _context = context;
        _connectionString = configuration.GetValue<string>("ConnectionStrings:SAConnectionString");
        _tableName = configuration.GetValue<string>("ConnectionStrings:AzureTableName");
    }

    private TableClient GetTableClient()
    {
        var serviceClient = new TableServiceClient(_connectionString);
        var tableClient = serviceClient.GetTableClient(_tableName);

        tableClient.CreateIfNotExists();
        return tableClient;
    }

    [HttpGet("ListarTodos")]
    public IActionResult ListarTodos()
    {
        var funcionarios = _context.Funcionarios.ToList();

        if (funcionarios == null || !funcionarios.Any())
            return NotFound();

        return Ok(funcionarios);
    }

    [HttpGet("{id}")]
    public IActionResult ObterPorId(int id)
    {
        var funcionario = _context.Funcionarios.Find(id);

        if (funcionario == null)
            return NotFound();

        return Ok(funcionario);
    }


    [HttpGet("{id}/Log")]
    public IActionResult ObterLogPorId(int id)
    {
        var funcionario = _context.Funcionarios.Find(id);

        if (funcionario == null)
            return NotFound();
        

        var tableClient = GetTableClient();

        // Busca todos os logs do funcionário pelo PartitionKey (id)
        var funcionarioLog = tableClient.Query<FuncionarioLog>(log => log.PartitionKey == id.ToString()).ToList();

        if (funcionarioLog == null || !funcionarioLog.Any())
        return NotFound();

        return Ok(funcionarioLog);
    }

    [HttpPost]
    public IActionResult Criar(Funcionario funcionario)
    {
        _context.Funcionarios.Add(funcionario);
        // TODO: Chamar o método SaveChanges do _context para salvar no Banco SQL
        _context.SaveChanges();

        var tableClient = GetTableClient();
        var funcionarioLog = new FuncionarioLog(funcionario, TipoAcao.Inclusao, Guid.NewGuid().ToString());

        // TODO: Chamar o método UpsertEntity para salvar no Azure Table
        tableClient.UpsertEntity(funcionarioLog);

        return CreatedAtAction(nameof(ObterPorId), new { id = funcionario.Id }, funcionario);
    }

    [HttpPut("{id}")]
    public IActionResult Atualizar(int id, Funcionario funcionario)
    {
        var funcionarioBanco = _context.Funcionarios.Find(id);

        if (funcionarioBanco == null)
            return NotFound();

        funcionarioBanco.Nome = funcionario.Nome;
        funcionarioBanco.Endereco = funcionario.Endereco;
        // TODO: As propriedades estão incompletas
        funcionarioBanco.Salario = funcionario.Salario;
        funcionarioBanco.Departamento = funcionario.Departamento;
        funcionarioBanco.DataAdmissao = funcionario.DataAdmissao;

        // TODO: Chamar o método de Update do _context.Funcionarios para salvar no Banco SQL
        _context.Funcionarios.Update(funcionarioBanco);
        _context.SaveChanges();        

        var tableClient = GetTableClient();
        var funcionarioLog = new FuncionarioLog(funcionarioBanco, TipoAcao.Atualizacao, Guid.NewGuid().ToString());

        // TODO: Chamar o método UpsertEntity para salvar no Azure Table
        tableClient.UpsertEntity(funcionarioLog);

        return Ok();
    }

    [HttpDelete("{id}")]
    public IActionResult Deletar(int id)
    {
        var funcionarioBanco = _context.Funcionarios.Find(id);

        if (funcionarioBanco == null)
            return NotFound();

        // TODO: Chamar o método de Remove do _context.Funcionarios para salvar no Banco SQL
        _context.Funcionarios.Remove(funcionarioBanco);
        _context.SaveChanges();

        var tableClient = GetTableClient();
        var funcionarioLog = new FuncionarioLog(funcionarioBanco, TipoAcao.Remocao, Guid.NewGuid().ToString());

        // TODO: Chamar o método UpsertEntity para salvar no Azure Table
        tableClient.UpsertEntity(funcionarioLog);

        return NoContent();
    }
}
