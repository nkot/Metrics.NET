﻿
using System;
using FluentAssertions;
using Metrics.Core;
using Xunit;

namespace Metrics.Tests
{
    public class HealthCheckTests
    {
        [Fact]
        public void HealthCheckReturnsResultWithCorrectName()
        {
            string name = "test";
            new HealthCheck(name, () => { }).Execute().Name.Should().Be(name);
            new HealthCheck(name, () => { return "string"; }).Execute().Name.Should().Be(name);
            new HealthCheck(name, () => { HealthCheckResult.Healthy(); }).Execute().Name.Should().Be(name);
        }

        [Fact]
        public void HealthCheckSuccessIfActionDoesNotThrow()
        {
            string name = "test";
            new HealthCheck(name, () => { }).Execute().Check.IsHealthy.Should().BeTrue();
            new HealthCheck(name, () => { return "string"; }).Execute().Check.IsHealthy.Should().BeTrue();
            new HealthCheck(name, () => { HealthCheckResult.Healthy(); }).Execute().Check.IsHealthy.Should().BeTrue();
        }

        private static void ThrowException()
        {
            throw new InvalidOperationException();
        }

        [Fact]
        public void HealthCheckFailedIfActionThrows()
        {
            string name = "test";
            new HealthCheck(name, () => ThrowException()).Execute().Check.IsHealthy.Should().BeFalse();
            new HealthCheck(name, () => { ThrowException(); return "string"; }).Execute().Check.IsHealthy.Should().BeFalse();
            new HealthCheck(name, () => { ThrowException(); HealthCheckResult.Healthy(); }).Execute().Check.IsHealthy.Should().BeFalse();
        }

        [Fact]
        public void HealthCheckFailedIfResultUnhealthy()
        {
            new HealthCheck("test", () => HealthCheckResult.Unhealthy()).Execute().Check.IsHealthy.Should().BeFalse();
        }

        [Fact]
        public void HealthCheckReturnsCorrectMessage()
        {
            string message = "message";
            new HealthCheck("test", () => HealthCheckResult.Unhealthy(message)).Execute().Check.Message.Should().Be(message);
            new HealthCheck("test", () => HealthCheckResult.Healthy(message)).Execute().Check.Message.Should().Be(message);
            new HealthCheck("test", () => { return message; }).Execute().Check.Message.Should().Be(message);
        }
    }
}
