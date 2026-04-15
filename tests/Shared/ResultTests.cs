using FluentAssertions;
using IMS.Modular.Shared.Domain;

namespace IMS.Modular.Tests.Shared;

/// <summary>
/// US-026: Unit tests for the Result pattern (Railway-Oriented Programming).
/// </summary>
public class ResultTests
{
    [Fact]
    public void Success_IsSuccess_True_AndValueSet()
    {
        var result = Result<string>.Success("hello");
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_IsSuccess_False_AndErrorSet()
    {
        var result = Result<string>.Failure("Something went wrong", 400);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Something went wrong");
        result.ErrorCode.Should().Be(400);
    }

    [Fact]
    public void NotFound_ErrorCode_Is404()
    {
        var result = Result<string>.NotFound("Not found");
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(404);
    }

    [Fact]
    public void Unauthorized_ErrorCode_Is401()
    {
        var result = Result<string>.Unauthorized("Unauthorized");
        result.ErrorCode.Should().Be(401);
    }

    [Fact]
    public void Forbidden_ErrorCode_Is403()
    {
        var result = Result<string>.Forbidden("Forbidden");
        result.ErrorCode.Should().Be(403);
    }

    [Fact]
    public void Conflict_ErrorCode_Is409()
    {
        var result = Result<string>.Conflict("Conflict");
        result.ErrorCode.Should().Be(409);
    }

    [Fact]
    public void Unprocessable_ErrorCode_Is422()
    {
        var result = Result<string>.Unprocessable("Unprocessable");
        result.ErrorCode.Should().Be(422);
    }
}
