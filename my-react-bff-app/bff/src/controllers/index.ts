import { Request, Response } from 'express';

export const getExampleData = (req: Request, res: Response) => {
    res.json({ message: 'This is example data from the BFF!' });
};

// Additional controller functions can be added here as needed.