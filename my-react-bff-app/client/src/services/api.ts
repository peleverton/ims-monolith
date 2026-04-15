import axios from 'axios';

const apiClient = axios.create({
    baseURL: 'http://localhost:3000/api', // Adjust the base URL as needed
    headers: {
        'Content-Type': 'application/json',
    },
});

export const fetchData = async (endpoint: string) => {
    try {
        const response = await apiClient.get(endpoint);
        return response.data;
    } catch (error) {
        console.error('Error fetching data:', error);
        throw error;
    }
};

export const postData = async (endpoint: string, data: any) => {
    try {
        const response = await apiClient.post(endpoint, data);
        return response.data;
    } catch (error) {
        console.error('Error posting data:', error);
        throw error;
    }
};

// Add more API functions as needed