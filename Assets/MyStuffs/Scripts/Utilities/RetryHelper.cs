using System;
using System.Threading.Tasks;
using Amazon.Runtime;
using UnityEngine;

public static class RetryHelper
{
    // Retries any async AWS operation with exponential backoff
    // maxRetries: how many times to retry before giving up
    // initialDelayMs: how long to wait before the first retry
    public static async Task<T> ExecuteWithRetry<T>(
        Func<Task<T>> operation,
        string operationName,
        int maxRetries = 3,
        int initialDelayMs = 1000)
    {
        int delayMs = initialDelayMs;

        for (int attempt = 1; attempt <= maxRetries + 1; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (AmazonServiceException e) when (IsRetryable(e) && attempt <= maxRetries)
            {
                Debug.LogWarning($"[RetryHelper] {operationName} failed (attempt {attempt}). Retrying in {delayMs}ms. Error: {e.Message}");
                await Task.Delay(delayMs);
                delayMs *= 2;
            }
            catch (AmazonClientException e) when (attempt <= maxRetries)
            {
                // Network error, worth retrying
                Debug.LogWarning($"[RetryHelper] {operationName} network error (attempt {attempt}). Retrying in {delayMs}ms. Error: {e.Message}");
                await Task.Delay(delayMs);
                delayMs *= 2;
            }
        }

        // Should never reach here but satisfies compiler
        throw new Exception($"{operationName} failed after {maxRetries} retries.");
    }

    // Determines if an AWS error is worth retrying
    private static bool IsRetryable(AmazonServiceException e)
    {
        // 500 and 503 are server errors worth retrying
        // 429 is too many requests, worth retrying after a delay
        return e.StatusCode == System.Net.HttpStatusCode.InternalServerError
            || e.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
            || e.StatusCode == (System.Net.HttpStatusCode)429;
    }
}