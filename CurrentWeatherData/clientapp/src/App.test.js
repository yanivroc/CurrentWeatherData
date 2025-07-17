import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from './App';

// Mock environment variables for consistent testing
process.env.REACT_APP_API_HOST = 'https://mockapi.com';
process.env.REACT_APP_API_KEY = 'test-api-key-123';

// Mock the global fetch API
const mockFetch = jest.spyOn(global, 'fetch');

// Clear mocks before each test to ensure isolation
beforeEach(() => {
    mockFetch.mockClear();
});

// Helper function to find the description text more robustly
const findDescriptionText = (description) => {
    // Find the span containing "Description:" and then check its parent's text content
    return screen.getByText('Description:').closest('p').textContent.includes(description);
};


// Test Case 1: Pass city and country and match with returned description
test('fetches and displays weather description for valid input', async () => {
    // Mock a successful API response
    mockFetch.mockImplementationOnce(() =>
        Promise.resolve({
            ok: true,
            json: () => Promise.resolve({ description: 'clear sky' }),
            text: () => Promise.resolve(JSON.stringify({ description: 'clear sky' })),
            status: 200,
        })
    );

    render(<App />);

    // Simulate user typing into input fields
    await userEvent.type(screen.getByLabelText(/City:/i), 'London');
    await userEvent.type(screen.getByLabelText(/Country:/i), 'UK');

    // Simulate user clicking the button
    fireEvent.click(screen.getByRole('button', { name: /Get Weather/i }));

    // Assert that loading message appears
    expect(screen.getByText(/Loading weather data.../i)).toBeInTheDocument();

    // Wait for the asynchronous operation to complete and UI to update
    await waitFor(() => {
        // Use the helper function to correctly find the description text
        expect(findDescriptionText('clear sky')).toBe(true);
        // Assert that the city and country are displayed correctly in the result title
        expect(screen.getByText(/Weather in London, UK/i)).toBeInTheDocument();
        // Assert that input fields are cleared
        expect(screen.getByLabelText(/City:/i)).toHaveValue('');
        expect(screen.getByLabelText(/Country:/i)).toHaveValue('');
    });

    // Verify that fetch was called with the correct URL and headers
    expect(mockFetch).toHaveBeenCalledTimes(1);
    expect(mockFetch).toHaveBeenCalledWith(
        'https://mockapi.com/weather?city=London&country=UK',
        {
            method: 'GET',
            headers: {
                Accept: 'application/json',
                'X-Api-Key': 'test-api-key-123',
            },
        }
    );
});

// Test Case 2: Check for no API key (simulating unauthorized response from backend)
test('displays error when API key is missing or invalid', async () => {
    // Mock the API_KEY to be empty for this test
    process.env.REACT_APP_API_KEY = ''; // Temporarily set to empty for this test

    // Mock an unauthorized API response
    mockFetch.mockImplementationOnce(() =>
        Promise.resolve({
            ok: false,
            status: 401,
            text: () => Promise.resolve("API Key is missing. Please provide a valid 'X-Api-Key' header."),
        })
    );

    render(<App />);

    await userEvent.type(screen.getByLabelText(/City:/i), 'Paris');
    await userEvent.type(screen.getByLabelText(/Country:/i), 'FR');
    fireEvent.click(screen.getByRole('button', { name: /Get Weather/i }));

    await waitFor(() => {
        // Rely on the full error message, which implicitly confirms the "Error!" strong tag
        expect(screen.getByText(/HTTP error! Status: 401 - API Key is missing./i)).toBeInTheDocument();
        // We can still check for "Error!" if desired, but it's often redundant if the specific message is checked
        expect(screen.getByText('Error!')).toBeInTheDocument();
    });

    // Restore API_KEY for subsequent tests
    process.env.REACT_APP_API_KEY = 'test-api-key-123';
});


// Test Case 3: If more than 5 times then show rate limit exceeded
test('displays rate limit exceeded message after 5 successful requests', async () => {
    // Mock successful responses for the first 5 requests
    mockFetch.mockImplementation(() =>
        Promise.resolve({
            ok: true,
            json: () => Promise.resolve({ description: 'sunny' }),
            text: () => Promise.resolve(JSON.stringify({ description: 'sunny' })),
            status: 200,
        })
    );

    render(<App />);

    const cityInput = screen.getByLabelText(/City:/i);
    const countryInput = screen.getByLabelText(/Country:/i);
    const getWeatherButton = screen.getByRole('button', { name: /Get Weather/i });

    // Simulate 5 successful requests
    for (let i = 0; i < 5; i++) {
        // Clear inputs before typing for each iteration, as App component clears after successful fetch
        await userEvent.clear(cityInput);
        await userEvent.clear(countryInput);
        await userEvent.type(cityInput, `City${i}`);
        await userEvent.type(countryInput, `C${i}`);
        fireEvent.click(getWeatherButton);
        await waitFor(() => {
            // Use the helper function to correctly find the description text
            expect(findDescriptionText('sunny')).toBe(true);
        });
    }

    // Mock the 6th request to be rate-limited (429 status)
    mockFetch.mockImplementationOnce(() =>
        Promise.resolve({
            ok: false,
            status: 429,
            text: () => Promise.resolve("Hourly rate limit exceeded. Please try again later."),
        })
    );

    // Simulate the 6th request
    await userEvent.type(cityInput, 'OverLimitCity');
    await userEvent.type(countryInput, 'OL');
    fireEvent.click(getWeatherButton);

    await waitFor(() => {
        // Assert that the rate limit exceeded error message is displayed
        expect(screen.getByText(/HTTP error! Status: 429 - Hourly rate limit exceeded./i)).toBeInTheDocument();
        expect(screen.getByText('Error!')).toBeInTheDocument();
    });

    // Ensure fetch was called 6 times in total
    expect(mockFetch).toHaveBeenCalledTimes(6);
});

// Test Case 4: Test for handling empty city/country submission
test('displays error if city or country is empty on submission', async () => {
    render(<App />);

    // Try submitting with empty fields
    fireEvent.click(screen.getByRole('button', { name: /Get Weather/i }));

    // Assert that the error message is displayed
    await waitFor(() => {
        expect(screen.getByText('Error!')).toBeInTheDocument();
        expect(screen.getByText(/Please enter both city and country./i)).toBeInTheDocument();
    });

    // Ensure fetch was NOT called
    expect(mockFetch).not.toHaveBeenCalled();
});

// TEST CASE 5: API returns a generic server error (e.g., 500 Internal Server Error)
test('displays a generic error message for server errors (e.g., 500)', async () => {
    mockFetch.mockImplementationOnce(() =>
        Promise.resolve({
            ok: false,
            status: 500,
            text: () => Promise.resolve("Internal Server Error"),
        })
    );

    render(<App />);

    await userEvent.type(screen.getByLabelText(/City:/i), 'Berlin');
    await userEvent.type(screen.getByLabelText(/Country:/i), 'DE');
    fireEvent.click(screen.getByRole('button', { name: /Get Weather/i }));

    await waitFor(() => {
        expect(screen.getByText('Error!')).toBeInTheDocument();
        expect(screen.getByText(/HTTP error! Status: 500 - Internal Server Error/i)).toBeInTheDocument();
    });

    expect(mockFetch).toHaveBeenCalledTimes(1);
});

// TEST CASE 6: Network error (e.g., API is down, no internet connection)
test('displays network error message when fetch fails', async () => {
    // Mock fetch to throw an error, simulating a network issue
    mockFetch.mockImplementationOnce(() =>
        Promise.reject(new TypeError('Failed to fetch'))
    );

    render(<App />);

    await userEvent.type(screen.getByLabelText(/City:/i), 'Tokyo');
    await userEvent.type(screen.getByLabelText(/Country:/i), 'JP');
    fireEvent.click(screen.getByRole('button', { name: /Get Weather/i }));

    await waitFor(() => {
        expect(screen.getByText('Error!')).toBeInTheDocument();
        // Expecting the actual error message from TypeError
        expect(screen.getByText(/Failed to fetch/i)).toBeInTheDocument();
    });

    expect(mockFetch).toHaveBeenCalledTimes(1);
});

// TEST CASE 7: Clears previous results and errors when a new search is initiated
test('clears previous results and errors when a new search is initiated', async () => {
    // First, simulate a successful request
    mockFetch.mockImplementationOnce(() =>
        Promise.resolve({
            ok: true,
            json: () => Promise.resolve({ description: 'cloudy' }),
            text: () => Promise.resolve(JSON.stringify({ description: 'cloudy' })),
            status: 200,
        })
    );

    render(<App />);

    await userEvent.type(screen.getByLabelText(/City:/i), 'OldCity');
    await userEvent.type(screen.getByLabelText(/Country:/i), 'OC');
    fireEvent.click(screen.getByRole('button', { name: /Get Weather/i }));

    await waitFor(() => {
        // Use the helper function to correctly find the description text
        expect(findDescriptionText('cloudy')).toBe(true);
        expect(screen.queryByText('Error!')).not.toBeInTheDocument(); // Ensure no error is present
    });

    // Simulate an error for the next request
    mockFetch.mockImplementationOnce(() =>
        Promise.resolve({
            ok: false,
            status: 400,
            text: () => Promise.resolve("Bad Request"),
        })
    );

    // Simulate a new search
    await userEvent.type(screen.getByLabelText(/City:/i), 'NewCity');
    await userEvent.type(screen.getByLabelText(/Country:/i), 'NC');
    fireEvent.click(screen.getByRole('button', { name: /Get Weather/i }));

    // Assert that previous weather data is gone and loading message appears
    expect(screen.queryByText(/Description:/i)).not.toBeInTheDocument(); // Query for any description element
    expect(screen.getByText(/Loading weather data.../i)).toBeInTheDocument();

    await waitFor(() => {
        // Assert that the new error is displayed
        expect(screen.getByText('Error!')).toBeInTheDocument();
        expect(screen.getByText(/HTTP error! Status: 400 - Bad Request/i)).toBeInTheDocument();
    });
});