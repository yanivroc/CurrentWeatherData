import React, { useState } from 'react';
import './index.css'; // Ensure this import is present to load your CSS

// Main App component for the weather application
const App = () => {
    // State variables for user inputs, weather data, loading status, and errors
    const [city, setCity] = useState('');
    const [country, setCountry] = useState('');
    const [weatherData, setWeatherData] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    // API host address and the API key using environment variables
    const API_HOST = process.env.REACT_APP_API_HOST;
    const API_KEY = process.env.REACT_APP_API_KEY;

    // Function to handle fetching weather data from the backend
    const fetchWeatherData = async () => {
        setLoading(true); // Set loading state to true
        setError(null); // Clear any previous errors
        setWeatherData(null); // Clear any previous weather data

        try {
            // Construct the API URL with city and country parameters
            const url = `${API_HOST}/weather?city=${encodeURIComponent(city)}&country=${encodeURIComponent(country)}`;

            // Make the fetch request, including the X-Api-Key header
            const response = await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'X-Api-Key': API_KEY // Include the API key here
                }
            });

            // Check if the response was successful (status code 2xx)
            if (!response.ok) {
                // If not successful, parse the error message from the response body
                const errorText = await response.text();
                throw new Error(`HTTP error! Status: ${response.status} - ${errorText}`);
            }

            // Parse the JSON response
            const data = await response.json();
            setWeatherData(data); // Set the weather data
        } catch (err) {
            // Catch and set any errors that occur during the fetch operation
            console.error('Error fetching weather data:', err);
            setError(err.message || 'An unknown error occurred.');
        } finally {
            setLoading(false); // Reset loading state
        }
    };

    // Handle form submission
    const handleSubmit = (e) => {
        e.preventDefault(); // Prevent default form submission behavior
        if (city && country) {
            fetchWeatherData(); // Fetch data if both fields are filled
        } else {
            setError('Please enter both city and country.');
        }
    };

    return (
        <div className="weather-app-container">
            <h1 className="app-title">
                Weather App
            </h1>

            {/* Input form for city and country */}
            <form onSubmit={handleSubmit}>
                <div className="input-group">
                    <label htmlFor="city" className="input-label">
                        City:
                    </label>
                    <input
                        type="text"
                        id="city"
                        className="text-input"
                        value={city}
                        onChange={(e) => setCity(e.target.value)}
                        placeholder="e.g., Melbourne"
                        required
                    />
                </div>
                <div className="input-group">
                    <label htmlFor="country" className="input-label">
                        Country:
                    </label>
                    <input
                        type="text"
                        id="country"
                        className="text-input" // Use new CSS class
                        value={country}
                        onChange={(e) => setCountry(e.target.value)}
                        placeholder="e.g., AU"
                        required
                    />
                </div>
                <button
                    type="submit"
                    className="submit-button" // Use new CSS class
                    disabled={loading} // Disable button when loading
                >
                    {loading ? 'Fetching Weather...' : 'Get Weather'}
                </button>
            </form>

            {/* Display loading, error, or weather data */}
            {loading && (
                <div className="loading-message">Loading weather data...</div> 
      )}

            {error && (
                <div className="error-message" role="alert"> 
                    <strong>Error!</strong>
                    <span>{error}</span>
                </div>
            )}

            {weatherData && (
                <div className="weather-result"> 
                    <h2 className="weather-result-title"> 
                        Weather in {city}, {country}
                    </h2>
                    <div>
                        <p className="weather-info-item">
                            <span>Description:</span> {weatherData.description}
                        </p>
                    </div>
                </div>
            )}
        </div>
    );
};

export default App;